using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SpacetimeRift
{
    /// <summary>
    /// 时空裂隙射出的裂隙光束箭,不受重力,最多命中 3 次,每个敌人一次
    /// ai[0]=阶段(0 刚射出、1 等待瞬移、2 正常飞行、3 已瞬移) ai[1]=瞬移目标的 NPC 序号
    /// localAI[0]=已飞行的真实帧数 localAI[1]=上一次更新看到的阶段,各端据此补播瞬移特效、缩短余寿
    /// 刚射出时主人端沿飞行方向找路径上最近、中间没有墙的可追踪敌人,找到就在 6 帧后瞬移到它身上;等待中先撞上敌人就取消
    /// 瞬移起点随 SendExtraAI 同步;首次命中时主人端放引力爆炸,瞬移过的把起点交给爆炸画裂隙
    /// </summary>
    public class SpacetimeRiftArrow : ModProjectile
    {
        private const int Fresh = 0;
        private const int Pending = 1;
        private const int Flying = 2;
        private const int Teleported = 3;

        //每帧多跑 1 次更新
        private const int ExtraUpdates = 1;
        //最多命中次数,策划「总共可以穿透 3 次」
        private const int MaxHits = 3;
        //寿命(更新),约 1.5 秒;瞬移后穿出目标还能飞的更新数
        private const int Lifetime = 180;
        private const int AfterTeleportLife = 40;
        //路径检测最远 60 格(像素)、检测线宽(像素)
        private const float RayLength = 960f;
        private const float RayWidth = 12f;
        //射出后第几帧瞬移,按真实帧计,不受豆奶影响
        private const int TeleportDelay = 6;
        //引力爆炸 125%
        private const float BlastMult = 1.25f;
        //拖影: 残影数量(每次更新记一个)、起始不透明度、最末一个残影的横向宽度
        private const int TrailLength = 10;
        private const float TrailOpacity = 0.55f;
        private const float TrailMinWidth = 0.5f;
        //光晕(Glow2 原图 256 像素)不透明度
        private const float GlowOpacity = 0.7f;
        //瞬移起点的闪光(Glow2)大小与存活帧数
        private const float DepartFlashScale = 0.22f;
        private const int DepartFlashLife = 8;
        //瞬移起点与撞墙时的火花(GlowSpark 原图 2048 像素): 数量、速度区间(像素/帧)、大小、存活帧数、向后散开的角度(弧度)
        private const int DepartSparks = 4;
        private const int WallSparks = 3;
        private const float SparkSpeedMin = 3f;
        private const float SparkSpeedMax = 6f;
        private const float SparkScale = 0.025f;
        private const int SparkLife = 12;
        private const float SparkSpread = MathHelper.PiOver2;

        //光晕沿箭身拉长的横纵缩放
        private static readonly Vector2 GlowScale = new(0.12f, 0.4f);
        private static readonly Vector2 SparkSquash = new(0.5f, 1.4f);
        private static readonly Color TrailTint = new(170, 110, 255);
        private static readonly Color GlowPurple = new(136, 35, 159);
        private static readonly Color RiftGold = new(255, 214, 120);
        private static readonly Vector3 ArrowLight = new(0.4f, 0.2f, 0.6f);

        private Vector2 riftStart;
        //只在主人端有意义: 引力爆炸已经放过
        private bool blasted;

        private int State {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private NPC Target {
            get {
                int index = (int)Projectile.ai[1];
                if (index < 0 || index >= Main.maxNPCs)
                    return null;
                NPC npc = Main.npc[index];
                return npc.CanBeChasedBy(Projectile) ? npc : null;
            }
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.arrow = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = MaxHits;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            if (Projectile.localAI[1] != State) {
                if (State == Teleported) {
                    ClearTrail();
                    Projectile.timeLeft = Math.Min(Projectile.timeLeft, AfterTeleportLife);
                    if (!Main.dedServ)
                        DepartBurst();
                }
                Projectile.localAI[1] = State;
            }
            Projectile.tileCollide = State != Teleported;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.owner == Main.myPlayer) {
                if (State == Fresh)
                    Scan();
                else if (State == Pending && Projectile.localAI[0] >= TeleportDelay)
                    Teleport();
            }
            if (Projectile.FinalExtraUpdate())
                Projectile.localAI[0]++;
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, ArrowLight);
        }

        /// <summary>可追踪已包含非友方、可受伤、非假人;取路径上最先碰到的那个,入口点之前不能有墙</summary>
        private void Scan() {
            Vector2 start = Projectile.Center;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 end = start + direction * RayLength;
            int best = -1;
            float bestDistance = RayLength;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;
                float distance = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.position, npc.Size, start, end, RayWidth, ref distance) || distance >= bestDistance)
                    continue;
                if (!Collision.CanHitLine(start, 0, 0, start + direction * distance, 0, 0))
                    continue;
                best = npc.whoAmI;
                bestDistance = distance;
            }
            State = best >= 0 ? Pending : Flying;
            Projectile.ai[1] = best;
            Projectile.netUpdate = true;
        }

        /// <summary>落点在目标中心往回一步速度,本次更新移动后正好到中心,紧接着的伤害结算必中</summary>
        private void Teleport() {
            NPC target = Target;
            if (target == null) {
                State = Flying;
                Projectile.netUpdate = true;
                return;
            }
            riftStart = Projectile.Center;
            Projectile.Center = target.Center - Projectile.velocity;
            ClearTrail();
            Projectile.tileCollide = false;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, AfterTeleportLife);
            State = Teleported;
            Projectile.netUpdate = true;
        }

        private void ClearTrail() {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        private void DepartBurst() {
            PRTLoader.NewParticle<PRT_ShineParticle>(riftStart, Vector2.Zero, GlowPurple, DepartFlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, DepartFlashLife);
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < DepartSparks; i++) {
                Vector2 velocity = back.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(riftStart, velocity, RiftGold, SparkScale).Configure(false, SparkLife, SparkSquash, true);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (State == Pending) {
                State = Flying;
                Projectile.netUpdate = true;
            }
            if (blasted)
                return;
            blasted = true;
            float rift = State == Teleported ? 1f : 0f;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SpacetimeRiftBlast>(), (int)(Projectile.damage * BlastMult), Projectile.knockBack, Projectile.owner, rift, riftStart.X, riftStart.Y);
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(riftStart);

        public override void ReceiveExtraAI(BinaryReader reader) => riftStart = reader.ReadVector2();

        public override void OnKill(int timeLeft) {
            if (timeLeft <= 0 || Main.dedServ)
                return;
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < WallSparks; i++) {
                Vector2 velocity = back.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, velocity, RiftGold, SparkScale).Configure(false, SparkLife, SparkSquash, true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            for (int i = TrailLength - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)TrailLength;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, null, TrailTint * (fade * TrailOpacity), Projectile.rotation, origin, Projectile.scale * new Vector2(MathHelper.Lerp(TrailMinWidth, 1f, fade), 1f), SpriteEffects.None);
            }
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D glow = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(glow, center, null, (GlowPurple with { A = 0 }) * GlowOpacity, Projectile.rotation, glow.Size() / 2f, GlowScale * Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
