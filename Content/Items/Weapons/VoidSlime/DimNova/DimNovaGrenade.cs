using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.DimNova
{
    /// <summary>
    /// 黯淡新星掷出的凝胶手雷,只由主人端生成
    /// ai[0]=阶段(0 弹跳、1 原地等待、2 弹射) ai[1]=本阶段已过帧数 ai[2]=弹射目标的 NPC 序号
    /// localAI[0]=上一帧看到的阶段,各端据此补播弹射特效
    /// 碰到敌人、等满时间或弹射超时就炸,爆炸、碎片和凝胶团都在 OnKill 里由主人端生成
    /// </summary>
    public class DimNovaGrenade : ModProjectile
    {
        private const int Bouncing = 0;
        private const int Waiting = 1;
        private const int Launched = 2;

        //掷出后多久开始找敌人、找不到时最多再等多久(帧)
        private const int ArmFrames = 60;
        private const int WaitFrames = 180;
        //索敌半径 100 格(像素)
        private const float SeekRange = 1600f;
        //生命上限不超过这个的是原版小动物,碰到不算撞上敌人
        private const int CritterLifeMax = 5;
        //弹跳手感同原版弹力手榴弹: 出手 10 帧后受重力,反弹留 90%,分速度不到 1 像素/帧就不再弹,落地摩擦 0.97
        private const int GravityDelay = 10;
        private const float Gravity = 0.2f;
        private const float MaxFallSpeed = 16f;
        private const float Bounce = 0.9f;
        private const float BounceMinSpeed = 1f;
        private const float GroundFriction = 0.97f;
        private const float RestSpeed = 0.01f;
        //滚动时每像素水平速度转多少(弧度/帧),弹射时固定自转
        private const float SpinPerSpeed = 0.1f;
        private const float LaunchSpin = 0.35f;
        //原地等待时每帧保留的速度比例
        private const float WaitBrake = 0.85f;
        //弹射速度(像素/帧)、最长飞行时间(帧)
        private const float LaunchSpeed = 28f;
        private const int LaunchMaxFrames = 90;
        //寿命只是兜底,正常不会走到
        private const int Lifetime = ArmFrames + WaitFrames + LaunchMaxFrames + 30;
        //大爆炸 600%,半径 6 格(像素)
        private const float BlastMult = 6f;
        private const float BlastRadius = 96f;
        //碎片 5 发,各 50%
        private const int ShardCount = 5;
        private const float ShardMult = 0.5f;
        //凝胶团 2 个,各 100%;生成时往斜上方两侧漂出的速度(像素/帧)与张开的角度(弧度)
        private const int RemnantCount = 2;
        private const float RemnantMult = 1f;
        private const float RemnantDrift = 3f;
        private const float RemnantSpread = 1.6f;
        //本体半自发光,剩下的跟环境光
        private const float SelfLight = 0.45f;
        //光晕(Glow2 原图 256 像素): 平时与待爆时的大小、不透明度、呼吸快慢(弧度/帧)
        private const float GlowScale = 0.22f;
        private const float GlowScaleArmed = 0.32f;
        private const float GlowOpacity = 0.45f;
        private const float GlowOpacityArmed = 0.85f;
        private const float PulseSpeed = 0.12f;
        private const float PulseSpeedMax = 0.7f;
        //光晕随呼吸缩放的下限与幅度,待爆白芯相对光晕的大小
        private const float PulseScaleMin = 0.85f;
        private const float PulseScaleRange = 0.3f;
        private const float CoreScale = 0.45f;
        //弹射拖影: 残影数量、起始不透明度、最末一个残影的缩放
        private const int TrailLength = 8;
        private const float TrailOpacity = 0.55f;
        private const float TrailMinScale = 0.6f;
        //弹跳声: 撞击分速度高于 3 像素/帧才响
        private const float BounceSoundSpeed = 3f;
        private const float BounceVolume = 0.22f;
        private const float BouncePitchMin = 1.6f;
        private const float BouncePitchMax = 1.9f;
        private const int BounceInstances = 3;
        //弹射声用原版女王史莱姆的凝胶弹,调高音高
        private const float LaunchVolume = 0.55f;
        private const float LaunchPitch = 0.25f;
        private const float LaunchPitchRange = 0.1f;
        private const int LaunchInstances = 3;
        //弹射瞬间向后喷的冰屑: 粒数、散开角(弧度)、速度区间(像素/帧)、透明度、大小
        private const int LaunchDust = 8;
        private const float LaunchDustSpread = 0.6f;
        private const float LaunchDustSpeedMin = 1f;
        private const float LaunchDustSpeedMax = 4f;
        private const int LaunchDustAlpha = 100;
        private const float LaunchDustScale = 1.3f;

        private static readonly Color GlowBlue = new(110, 170, 255);
        private static readonly Color GlowWhite = new(223, 248, 255);
        private static readonly Vector3 GrenadeLight = new(0.25f, 0.45f, 0.8f);

        //呼吸相位只管好看,各端各算
        private float pulsePhase;
        //主人端记下撞上的敌人,碎片收紧时朝它收
        private int touchedNPC = -1;

        private int State {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private ref float Timer => ref Projectile.ai[1];

        private NPC Target {
            get {
                int index = (int)Projectile.ai[2];
                if (index < 0 || index >= Main.maxNPCs)
                    return null;
                NPC npc = Main.npc[index];
                return npc.CanBeChasedBy(Projectile) ? npc : null;
            }
        }

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/VoidSlime/DimNova/DimNova";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;

        /// <summary>给手雷和凝胶团判断碰没碰到敌人;假人也算,只是不会被当成追踪目标,同原版惯例</summary>
        internal static int TouchedEnemy(Rectangle box) {
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.friendly || npc.dontTakeDamage || npc.lifeMax <= CritterLifeMax)
                    continue;
                if (box.Intersects(npc.Hitbox))
                    return npc.whoAmI;
            }
            return -1;
        }

        public override void AI() {
            if (Projectile.localAI[0] != State) {
                if (State == Launched && !Main.dedServ)
                    LaunchBurst();
                Projectile.localAI[0] = State;
            }
            Projectile.tileCollide = State != Launched;
            Timer++;
            bool mine = Projectile.owner == Main.myPlayer;
            switch (State) {
                case Bouncing:
                    Fall();
                    if (mine && Timer >= ArmFrames && !TryLaunch()) {
                        State = Waiting;
                        Timer = 0f;
                        Projectile.netUpdate = true;
                    }
                    break;
                case Waiting:
                    Projectile.velocity *= WaitBrake;
                    if (mine && !TryLaunch() && Timer >= WaitFrames) {
                        Projectile.Kill();
                        return;
                    }
                    break;
                default:
                    Fly();
                    if (mine && Timer >= LaunchMaxFrames) {
                        Projectile.Kill();
                        return;
                    }
                    break;
            }
            if (mine) {
                touchedNPC = TouchedEnemy(Projectile.Hitbox);
                if (touchedNPC >= 0) {
                    Projectile.Kill();
                    return;
                }
            }
            if (!Main.dedServ) {
                float progress = State == Waiting ? Timer / WaitFrames : 0f;
                pulsePhase += MathHelper.Lerp(PulseSpeed, PulseSpeedMax, progress);
                Lighting.AddLight(Projectile.Center, GrenadeLight);
            }
        }

        private void Fall() {
            if (Projectile.velocity.Y == 0f) {
                Projectile.velocity.X *= GroundFriction;
                if (Math.Abs(Projectile.velocity.X) < RestSpeed)
                    Projectile.velocity.X = 0f;
            }
            if (Timer > GravityDelay)
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);
            Projectile.rotation += Projectile.velocity.X * SpinPerSpeed;
        }

        private bool TryLaunch() {
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, SeekRange);
            if (target == null)
                return false;
            State = Launched;
            Timer = 0f;
            Projectile.ai[2] = target.whoAmI;
            Projectile.velocity = Projectile.DirectionTo(target.Center) * LaunchSpeed;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
            return true;
        }

        private void Fly() {
            NPC target = Target;
            if (target == null && Projectile.owner == Main.myPlayer) {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, SeekRange);
                if (target != null) {
                    Projectile.ai[2] = target.whoAmI;
                    Projectile.netUpdate = true;
                }
            }
            if (target != null)
                Projectile.velocity = Projectile.DirectionTo(target.Center) * LaunchSpeed;
            Projectile.rotation += LaunchSpin * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            bool hitX = Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > BounceMinSpeed;
            bool hitY = Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > BounceMinSpeed;
            if (hitX)
                Projectile.velocity.X = -oldVelocity.X * Bounce;
            if (hitY)
                Projectile.velocity.Y = -oldVelocity.Y * Bounce;
            float impact = Math.Max(hitX ? Math.Abs(oldVelocity.X) : 0f, hitY ? Math.Abs(oldVelocity.Y) : 0f);
            if (impact > BounceSoundSpeed)
                CEUtils.PlaySound("ksLand", Main.rand.NextFloat(BouncePitchMin, BouncePitchMax), Projectile.Center, BounceInstances, BounceVolume);
            return false;
        }

        private void LaunchBurst() {
            SoundEngine.PlaySound(SoundID.Item154 with { Volume = LaunchVolume, Pitch = LaunchPitch, PitchVariance = LaunchPitchRange, MaxInstances = LaunchInstances, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, Projectile.Center);
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < LaunchDust; i++) {
                Vector2 velocity = back.RotatedByRandom(LaunchDustSpread) * Main.rand.NextFloat(LaunchDustSpeedMin, LaunchDustSpeedMax);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, velocity, LaunchDustAlpha, default, LaunchDustScale);
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) {
            if (Projectile.owner != Main.myPlayer)
                return;
            IEntitySource source = Projectile.GetSource_FromThis();
            Vector2 center = Projectile.Center;
            int damage = Projectile.damage;
            float knockback = Projectile.knockBack;
            Projectile.NewProjectile(source, center, Vector2.Zero, ModContent.ProjectileType<DimNovaBlast>(), (int)(damage * BlastMult), knockback, Projectile.owner, BlastRadius, DimNovaBlast.Big);

            int focus = touchedNPC;
            if (focus < 0 && State == Launched && Target != null)
                focus = (int)Projectile.ai[2];
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ShardCount; i++) {
                Vector2 direction = (start + MathHelper.TwoPi * i / ShardCount).ToRotationVector2();
                Projectile.NewProjectile(source, center, direction, ModContent.ProjectileType<DimNovaShard>(), (int)(damage * ShardMult), knockback, Projectile.owner, focus, center.X, center.Y);
            }

            for (int i = 0; i < RemnantCount; i++) {
                float angle = -MathHelper.PiOver2 + (i - (RemnantCount - 1) / 2f) * RemnantSpread;
                Projectile.NewProjectile(source, center, angle.ToRotationVector2() * RemnantDrift, ModContent.ProjectileType<DimNovaRemnant>(), (int)(damage * RemnantMult), knockback, Projectile.owner);
            }
            DimNovaRemnant.EnforceCap(Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color body = Color.Lerp(lightColor, Color.White, SelfLight);

            if (State == Launched) {
                for (int i = TrailLength - 1; i > 0; i--) {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float fade = 1f - i / (float)TrailLength;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, pos, null, body * (fade * TrailOpacity), Projectile.oldRot[i], origin, Projectile.scale * MathHelper.Lerp(TrailMinScale, 1f, fade), SpriteEffects.None);
                }
            }

            bool armed = State != Bouncing;
            float pulse = 0.5f + 0.5f * MathF.Sin(pulsePhase);
            float glowScale = (armed ? GlowScaleArmed : GlowScale) * (PulseScaleMin + PulseScaleRange * pulse) * Projectile.scale;
            float glowOpacity = armed ? GlowOpacityArmed : GlowOpacity;
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 glowOrigin = glow.Size() / 2f;
            Main.EntitySpriteDraw(glow, center, null, (GlowBlue with { A = 0 }) * glowOpacity, 0f, glowOrigin, glowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, null, body, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            if (armed)
                Main.EntitySpriteDraw(glow, center, null, (GlowWhite with { A = 0 }) * (glowOpacity * pulse), 0f, glowOrigin, glowScale * CoreScale, SpriteEffects.None);
            return false;
        }
    }
}
