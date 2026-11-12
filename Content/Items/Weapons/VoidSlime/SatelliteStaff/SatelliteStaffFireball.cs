using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SatelliteStaff
{
    /// <summary>烈日火球:快速、轻微追踪、不穿透;ai[0] 目标 NPC 下标</summary>
    public class SatelliteStaffFireball : ModProjectile
    {
        #region 调参旋钮
        //判定边长(像素)、寿命(更新次数)
        public const int Size = 14;
        public const int Lifetime = 180;
        //追踪转向系数(只转向,不改速度)
        public const float HomingLerp = 0.06f;
        //拖尾:记录的旧位置数、头部半宽(像素);光晕直径(像素)
        public const int TrailLength = 10;
        public const float TrailHalfWidth = 7f;
        public const float GlowDiameter = 40f;
        //烈日灼烧时长(帧,3秒,策划)
        public const int BurnFrames = 180;
        //照明强度;拖尾强度;光晕强度
        public const float LightStrength = 0.7f;
        public const float TrailOpacity = 0.8f;
        public const float GlowOpacity = 0.6f;
        //飞行中的火尘:每次更新 1/N 的几率、向后飘的速度比例、缩放
        public const int DustChance = 4;
        public const float DustDrift = 0.2f;
        public const float DustScale = 1.3f;
        //消失时:闪烁的缩放、寿命(帧)、光晕缩放;火星数、速度(像素/帧)、缩放
        public const float KillSparkleScale = 0.5f;
        public const int KillSparkleLife = 12;
        public const float KillSparkleBloom = 1.2f;
        public const int KillDust = 6;
        public const float KillDustSpeed = 3f;
        //火星继承火球速度的比例(顺着飞行方向溅开,不是四散)
        public const float KillDustInherit = 0.3f;
        public const float KillDustScale = 1.5f;
        //出手音效的音量、音高随机幅度、同时发声上限
        public const float SoundVolume = 0.4f;
        public const float SoundPitchVariance = 0.2f;
        public const int SoundMaxInstances = 4;
        #endregion

        public static readonly Color Flame = new(255, 120, 36);
        public static readonly Color Core = new(255, 220, 110);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = SatelliteStaffSlime.FireballUpdates - 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = SoundVolume, PitchVariance = SoundPitchVariance, MaxInstances = SoundMaxInstances }, Projectile.Center);
                }
            }
            int index = (int)Projectile.ai[0];
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].CanBeChasedBy(Projectile)) {
                float speed = Projectile.velocity.Length();
                Vector2 want = Projectile.DirectionTo(Main.npc[index].Center) * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, HomingLerp).SafeNormalize(Vector2.UnitX) * speed;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Flame.ToVector3() * LightStrength);
            if (!Main.dedServ && Main.rand.NextBool(DustChance)) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * DustDrift, 0, default, DustScale);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<SunblazeBurn>(), BurnFrames);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Core, KillSparkleScale).Configure(Flame, KillSparkleLife, bloomScale: KillSparkleBloom);
            for (int i = 0; i < KillDust; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Projectile.velocity * KillDustInherit + Main.rand.NextVector2Circular(KillDustSpeed, KillDustSpeed), 0, default, KillDustScale);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            List<Vector2> points = new List<Vector2> { Projectile.Center };
            foreach (Vector2 old in Projectile.oldPos) {
                if (old != Vector2.Zero) {
                    points.Add(old + Projectile.Size / 2f);
                }
            }
            GodDraw.BeginAdditive();
            if (points.Count > 2) {
                GodDraw.Strip(GodDraw.SoftLine, points, t => TrailHalfWidth * (1f - t), t => GodDraw.Add(Color.Lerp(Core, Flame, t), TrailOpacity * (1f - t)));
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(Flame, GlowOpacity), 0f, GlowDiameter / GodDraw.Glow.Width);
            GodDraw.End();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() / 2f, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
