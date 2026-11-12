using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.NoontideRapier
{
    //金色七彩矢;ai[0] 目标 NPC 下标(-1 为无,只由主人端改),ai[1]/ai[2] 无目标时飞向的点(发射时的鼠标)
    public class NoontideRapierBolt : ModProjectile
    {
        //速度 px/更新,时长按更新数,extraUpdates = 1 时一帧两次更新
        public const float DamageMult = 1.5f;           //策划,150%
        public const float KnockbackMult = 0.5f;
        public const int ExtraUpdates = 1;
        public const int Lifetime = 180;
        public const int Size = 20;                     //碰撞箱边长,px
        public const float SpawnOffset = 24f;           //从手前多远发出,px
        public const float LaunchSpeed = 9f;
        public const float SpreadMin = 0.5f;            //出手偏角,弧度,左右交替
        public const float SpreadMax = 1.1f;
        public const float SeekRadius = 400f;           //以鼠标为圆心选目标,px(25 格)
        public const float ResearchRadius = 480f;       //丢了目标后以自身为圆心重找,px(30 格)
        public const float HomingSpeed = 18f;
        public const float HomingLerp = 0.1f;
        public const int HomingRamp = 24;               //追踪力度从 0 爬满的更新数,前段划出弧线
        public const float ArriveDistance = 32f;
        public const int FadeIn = 6;
        public const int FadeOut = 20;
        public const int TrailLength = 20;
        public const float DrawScale = 0.6f;            //873 贴图的显示缩放
        public const float TrailEndScale = 1.4f;        //原版 873 拖尾末端放大
        public const float CoreScale = 0.35f;
        public const float FlarePulse = 0.2f;           //原版 873 闪烁幅度与频率
        public const float FlarePulseSpeed = 12f * MathHelper.Pi;
        public const int DustChance = 3;
        public const float DustMinScale = 0.6f;
        public const float DustMaxScale = 0.9f;
        public const float DustDrift = 1.5f;
        public const float LaunchVolume = 0.3f;
        public const float LaunchPitch = 0.2f;
        public const float KillVolume = 0.35f;
        public const float SoundPitchVariance = 0.2f;
        public const int MaxSounds = 3;
        public const float LaunchSparkleScale = 0.6f;
        public const int LaunchSparkleLife = 12;
        public const float KillSparkleScale = 1f;
        public const float KillBloomScale = 2f;
        public const int KillSparkleLife = 14;
        public const int KillSparkCount = 6;
        public const float KillSparkSpread = 0.8f;
        public const float KillSparkMinSpeed = 4f;
        public const float KillSparkMaxSpeed = 10f;
        public const float KillSparkScale = 0.045f;
        public const int KillSparkLife = 14;
        public const int KillDustCount = 8;
        public const float KillDustSpeed = 4f;

        public static readonly Color TrailHead = new Color(255, 214, 90, 120);
        public static readonly Color TrailTail = new Color(255, 96, 20, 60);
        public static readonly Color GlowColor = new Color(255, 200, 80, 0);
        public static readonly Color CoreColor = new Color(255, 250, 225, 255);
        public static readonly Color FlareColor = new Color(255, 210, 110, 0);
        public static readonly Color SparkGold = new Color(255, 220, 120);
        public static readonly Color DustGold = new Color(255, 190, 60);
        public static readonly Vector2 FlareLong = new Vector2(0.25f, 2.2f);
        public static readonly Vector2 FlareShort = new Vector2(0.25f, 1f);
        public static readonly Vector2 SparkSquash = new Vector2(0.4f, 1f);
        public static readonly Vector3 LightColor = new Vector3(0.6f, 0.45f, 0.15f);

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.HallowBossRainbowStreak;

        private int TargetIndex {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private Vector2 FallbackPoint => new Vector2(Projectile.ai[1], Projectile.ai[2]);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f && !Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Item165 with { Volume = LaunchVolume, Pitch = LaunchPitch, PitchVariance = SoundPitchVariance, MaxInstances = MaxSounds }, Projectile.Center);
                PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, CoreColor, LaunchSparkleScale).Configure(GlowColor, LaunchSparkleLife);
            }
            Projectile.localAI[0]++;
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeIn, Projectile.localAI[0], true);

            NPC target = Main.npc.IndexInRange(TargetIndex) ? Main.npc[TargetIndex] : null;
            if (target != null && !target.CanBeChasedBy(Projectile))
                target = null;
            if (target == null && Main.myPlayer == Projectile.owner) {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, ResearchRadius);
                int index = target?.whoAmI ?? -1;
                if (index != TargetIndex) {
                    TargetIndex = index;
                    Projectile.netUpdate = true;
                }
            }

            Vector2 heading = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 desired = heading * HomingSpeed;
            if (target != null)
                desired = (target.Center - Projectile.Center).SafeNormalize(heading) * HomingSpeed;
            else if (Projectile.localAI[1] == 0f) {
                if (Projectile.Distance(FallbackPoint) < ArriveDistance)
                    Projectile.localAI[1] = 1f;
                else
                    desired = (FallbackPoint - Projectile.Center).SafeNormalize(heading) * HomingSpeed;
            }
            float homing = HomingLerp * Utils.GetLerpValue(0f, HomingRamp, Projectile.localAI[0], true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, homing);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, LightColor * Projectile.Opacity);
            if (!Main.dedServ && Main.rand.NextBool(DustChance)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, Main.rand.NextVector2Circular(DustDrift, DustDrift), 0, DustGold, Main.rand.NextFloat(DustMinScale, DustMaxScale));
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = KillVolume, PitchVariance = SoundPitchVariance, MaxInstances = MaxSounds }, Projectile.Center);
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, CoreColor, KillSparkleScale).Configure(GlowColor, KillSparkleLife, 0f, KillBloomScale);
            Vector2 heading = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < KillSparkCount; i++) {
                Vector2 vel = heading.RotatedByRandom(KillSparkSpread) * Main.rand.NextFloat(KillSparkMinSpeed, KillSparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, SparkGold, KillSparkScale).Configure(false, KillSparkLife, SparkSquash, true);
            }
            for (int i = 0; i < KillDustCount; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, Main.rand.NextVector2Circular(KillDustSpeed, KillDustSpeed), 0, DustGold, Main.rand.NextFloat(DustMinScale, DustMaxScale));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = tex.Size() / 2f;
            Vector2 offset = Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            float fade = Projectile.Opacity * Utils.GetLerpValue(0f, FadeOut, Projectile.timeLeft, true);
            int count = Projectile.oldPos.Length;
            for (int i = count - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float t = i / (float)count;
                Color c = Color.Lerp(TrailHead, TrailTail, t) * ((1f - t) * fade);
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + offset, null, c, Projectile.oldRot[i], origin, DrawScale * MathHelper.Lerp(1f, TrailEndScale, t), SpriteEffects.None);
            }

            Vector2 head = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(tex, head, null, GlowColor * fade, Projectile.rotation, origin, DrawScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, head, null, CoreColor * fade, Projectile.rotation, origin, DrawScale * CoreScale, SpriteEffects.None);
            Texture2D flare = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 flareOrigin = flare.Size() / 2f;
            float pulse = fade * (1f + FlarePulse * MathF.Cos(Main.GlobalTimeWrappedHourly * FlarePulseSpeed));
            Main.EntitySpriteDraw(flare, head, null, FlareColor * pulse, MathHelper.PiOver2, flareOrigin, FlareLong * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(flare, head, null, FlareColor * pulse, 0f, flareOrigin, FlareShort * pulse, SpriteEffects.None);
            return false;
        }
    }
}
