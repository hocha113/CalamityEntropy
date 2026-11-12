using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.DimNova
{
    /// <summary>
    /// 大爆炸留下的虚化凝胶团,只由主人端生成;同一玩家闲置的最多 10 个,超出时最早的消失
    /// ai[0]=阶段(0 闲置、1 追踪) ai[1]=追踪目标的 NPC 序号(-1 没有)
    /// localAI[0]=上一帧看到的阶段,各端据此改寿命、补播唤醒特效
    /// 闲置时不造成伤害;追踪中碰到敌人就由主人端放一次小爆炸
    /// </summary>
    public class DimNovaRemnant : ModProjectile
    {
        public const int Idle = 0;
        public const int Homing = 1;

        //同一玩家闲置凝胶团的上限
        public const int MaxIdle = 10;
        //闲置寿命 30 秒、追踪寿命 4 秒(帧)
        private const int IdleLife = 1800;
        private const int HomingLife = 240;
        //漂出后每帧保留的速度比例
        private const float IdleDrag = 0.9f;
        //索敌半径(像素)、追踪速度(像素/帧)、转向惯性(越大转得越慢)
        private const float SeekRange = 1600f;
        private const float HomingSpeed = 20f;
        private const float HomingInertia = 10f;
        //目标没了又找不到新的: 每帧保留的速度比例,慢到这个速度(像素/帧)就重新闲置
        private const float LostDrag = 0.92f;
        private const float LostStopSpeed = 1f;
        //小爆炸 100%,半径 3 格(像素)
        private const float BlastMult = 1f;
        private const float BlastRadius = 48f;
        //闲置时上下浮动的幅度(像素)与快慢(弧度/秒),各团按序号错开相位
        private const float BobHeight = 3f;
        private const float BobSpeed = 3.6f;
        private const float BobPhaseStep = 1.7f;
        //呼吸缩放幅度、晃动角度(弧度);呼吸与晃动相对上下浮动的快慢倍数
        private const float BreathScale = 0.06f;
        private const float SwayAngle = 0.15f;
        private const float BreathRate = 1.3f;
        private const float SwayRate = 0.7f;
        //虚化: 闲置与追踪时本体的不透明度、自发光比例;寿命最后 30 帧淡出
        private const float IdleOpacity = 0.5f;
        private const float HomingOpacity = 0.85f;
        private const float SelfLight = 0.6f;
        private const int FadeFrames = 30;
        //光晕(Glow2 原图 256 像素)大小与不透明度
        private const float GlowScale = 0.2f;
        private const float GlowOpacity = 0.3f;
        private const float GlowOpacityHoming = 0.8f;
        //追踪拖影: 残影数量、起始不透明度、最末一个残影的缩放
        private const int TrailLength = 6;
        private const float TrailOpacity = 0.45f;
        private const float TrailMinScale = 0.5f;
        //唤醒声用原版女王史莱姆的凝胶弹,调高音高;一次右键唤醒好几团,只放两声
        private const float WakeVolume = 0.35f;
        private const float WakePitch = 0.5f;
        private const float WakePitchRange = 0.15f;
        private const int WakeInstances = 2;
        //唤醒闪光(Glow2)大小与存活帧数;闲置消散时的冰屑数、速度(像素/帧)、透明度、大小
        private const float WakeFlashScale = 0.25f;
        private const int WakeFlashLife = 10;
        private const int FadeDust = 6;
        private const float FadeDustSpeed = 2f;
        private const int FadeDustAlpha = 120;
        private const float FadeDustScale = 1f;
        //追踪时的光照是闲置时的几倍
        private const float HomingLightMult = 2f;

        private static readonly Color GlowBlue = new(110, 170, 255);
        private static readonly Color WakeWhite = new(223, 248, 255);
        private static readonly Vector3 RemnantLight = new(0.12f, 0.22f, 0.45f);

        //只在主人端为 true: 这次死亡是撞上敌人引爆的
        private bool detonated;

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
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = IdleLife;
        }

        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;

        public static int CountIdle(int owner) {
            int type = ModContent.ProjectileType<DimNovaRemnant>();
            int count = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == owner && p.type == type && p.ai[0] == Idle)
                    count++;
            }
            return count;
        }

        /// <summary>主人端在生成新凝胶团后调用;同时生成的 timeLeft 相同,最小的就是最早的</summary>
        public static void EnforceCap(int owner) {
            int type = ModContent.ProjectileType<DimNovaRemnant>();
            while (CountIdle(owner) > MaxIdle) {
                Projectile oldest = null;
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (p.owner == owner && p.type == type && p.ai[0] == Idle && (oldest == null || p.timeLeft < oldest.timeLeft))
                        oldest = p;
                }
                if (oldest == null)
                    break;
                oldest.Kill();
            }
        }

        /// <summary>主人端右键时调用: 每个闲置的凝胶团各自锁定离自己最近的敌人,附近没有敌人的继续闲置</summary>
        public static void CommandAll(Player player) {
            int type = ModContent.ProjectileType<DimNovaRemnant>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner != player.whoAmI || p.type != type || p.ai[0] != Idle)
                    continue;
                NPC target = CEUtils.FindTarget_HomingProj(p, p.Center, SeekRange);
                if (target == null)
                    continue;
                p.ai[0] = Homing;
                p.ai[1] = target.whoAmI;
                p.netUpdate = true;
            }
        }

        public override void AI() {
            if (Projectile.localAI[0] != State) {
                Projectile.timeLeft = State == Homing ? HomingLife : IdleLife;
                if (State == Homing && !Main.dedServ)
                    Wake();
                Projectile.localAI[0] = State;
            }
            if (State == Idle) {
                Projectile.velocity *= IdleDrag;
            }
            else {
                Chase();
                if (Projectile.owner == Main.myPlayer && DimNovaGrenade.TouchedEnemy(Projectile.Hitbox) >= 0) {
                    detonated = true;
                    Projectile.Kill();
                    return;
                }
            }
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, State == Homing ? RemnantLight * HomingLightMult : RemnantLight);
        }

        private void Chase() {
            NPC target = Target;
            if (target == null && Projectile.owner == Main.myPlayer) {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, SeekRange);
                int index = target?.whoAmI ?? -1;
                if (index != (int)Projectile.ai[1]) {
                    Projectile.ai[1] = index;
                    Projectile.netUpdate = true;
                }
            }
            if (target != null) {
                Vector2 desired = Projectile.DirectionTo(target.Center) * HomingSpeed;
                Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + desired) / HomingInertia;
                return;
            }
            Projectile.velocity *= LostDrag;
            if (Projectile.owner == Main.myPlayer && Projectile.velocity.Length() < LostStopSpeed) {
                State = Idle;
                Projectile.netUpdate = true;
            }
        }

        private void Wake() {
            SoundEngine.PlaySound(SoundID.Item155 with { Volume = WakeVolume, Pitch = WakePitch, PitchVariance = WakePitchRange, MaxInstances = WakeInstances, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, Projectile.Center);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, WakeWhite, WakeFlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, WakeFlashLife);
        }

        public override void OnKill(int timeLeft) {
            if (detonated && Projectile.owner == Main.myPlayer) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DimNovaBlast>(), (int)(Projectile.damage * BlastMult), Projectile.knockBack, Projectile.owner, BlastRadius, DimNovaBlast.Small);
                return;
            }
            if (State != Idle || Main.dedServ)
                return;
            for (int i = 0; i < FadeDust; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, Main.rand.NextVector2Circular(FadeDustSpeed, FadeDustSpeed), FadeDustAlpha, default, FadeDustScale);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            bool homing = State == Homing;
            float phase = Main.GlobalTimeWrappedHourly * BobSpeed + Projectile.identity * BobPhaseStep;
            float fade = Math.Min(1f, Projectile.timeLeft / (float)FadeFrames);
            Color body = Color.Lerp(lightColor, Color.White, SelfLight) * ((homing ? HomingOpacity : IdleOpacity) * fade);
            float scale = Projectile.scale * (1f + BreathScale * MathF.Sin(phase * BreathRate));

            if (homing) {
                for (int i = TrailLength - 1; i > 0; i--) {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float trailFade = 1f - i / (float)TrailLength;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, pos, null, body * (trailFade * TrailOpacity), Projectile.oldRot[i], origin, scale * MathHelper.Lerp(TrailMinScale, 1f, trailFade), SpriteEffects.None);
                }
            }

            Vector2 bob = homing ? Vector2.Zero : Vector2.UnitY * (MathF.Sin(phase) * BobHeight);
            Vector2 center = Projectile.Center + bob - Main.screenPosition;
            Texture2D glow = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(glow, center, null, (GlowBlue with { A = 0 }) * ((homing ? GlowOpacityHoming : GlowOpacity) * fade), 0f, glow.Size() / 2f, GlowScale * scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, null, body, MathF.Sin(phase * SwayRate) * SwayAngle, origin, scale, SpriteEffects.None);
            return false;
        }
    }
}
