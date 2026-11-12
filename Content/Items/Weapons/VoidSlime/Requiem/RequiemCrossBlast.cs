using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    /// <summary>安息弹命中处升起的金色十字架,纯视觉</summary>
    public class RequiemCrossBlast : ModProjectile
    {
        public const int Lifetime = 40;             //帧
        public const int PopTime = 6;               //帧,弹出放大
        public const int FadeTime = 16;             //帧,末尾淡出
        public const float PopOvershoot = 1.25f;    //弹出时的最大缩放
        public const float SettleRate = 0.3f;       //每帧回落到 1 的插值比例
        public const float RiseSpeed = 0.4f;        //像素/帧,缓缓上升
        public static readonly Vector2 CrossOrigin = new(21f, 21f);     //像素,贴图十字交点
        public const int HitboxSize = 16;           //像素,不造成伤害
        public const float HaloScale = 1.3f;        //加色外轮廓相对本体缩放
        public const float HaloOpacity = 0.45f;
        public const float BloomScale = 0.5f;       //Glow2 缩放
        public const float BloomOpacity = 0.7f;

        public const float BellPitch = 0.75f;       //CEUtils.PlaySound 音高
        public const float BellVolume = 0.85f;
        public const float ShatterVolume = 0.6f;
        public const int SoundMaxInstances = 2;
        public const float ShakeStrength = 8f;
        public const float FlashScale = 0.6f;       //Glow2 缩放
        public const int FlashLife = 10;            //帧
        public const float RingStart = 0.2f;        //HollowCircleHardEdge 156 像素的缩放
        public const float RingEnd = 1.4f;
        public const int RingLife = 18;             //帧
        public const int Arms = 4;                  //十字架四臂,按 90 度分布
        public const int ArmSparks = 6;             //每条臂的火花数
        public const float ArmSpread = 0.25f;       //弧度
        public const float ArmSpeedMin = 6f;        //像素/帧
        public const float ArmSpeedMax = 14f;
        public const float LongArmBoost = 1.6f;     //十字架下臂更长,火花飞得更远
        public const int ArmSparkLife = 16;         //帧
        public const float LightStrength = 1.4f;

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Burst();
            }
            int age = Lifetime - Projectile.timeLeft;
            if (age < PopTime)
                Projectile.scale = PopOvershoot * MathF.Sin(MathHelper.PiOver2 * (age + 1) / PopTime);
            else
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 1f, SettleRate);
            Projectile.Opacity = Projectile.timeLeft < FadeTime ? Projectile.timeLeft / (float)FadeTime : 1f;
            Lighting.AddLight(Projectile.Center, RequiemVfx.Gold.ToVector3() * (LightStrength * Projectile.Opacity));
        }

        private void Burst() {
            if (Main.dedServ)
                return;
            Vector2 c = Projectile.Center;
            CEUtils.PlaySound("bell", BellPitch, c, SoundMaxInstances, BellVolume);
            CEUtils.PlaySound("holyshield_shatter", 1f, c, SoundMaxInstances, ShatterVolume);
            CEUtils.SetShake(c, ShakeStrength);
            PRTLoader.NewParticle<PRT_ShineParticle>(c, Vector2.Zero, RequiemVfx.GoldWhite, FlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            PRTLoader.NewParticle<PRT_PulseRing>(c, Vector2.Zero, RequiemVfx.Gold, RingStart).Configure(RingEnd, RingLife);
            for (int arm = 0; arm < Arms; arm++) {
                Vector2 dir = (MathHelper.TwoPi * arm / Arms).ToRotationVector2();
                float boost = dir.Y > 0f ? LongArmBoost : 1f;
                for (int i = 0; i < ArmSparks; i++) {
                    Vector2 v = dir.RotatedByRandom(ArmSpread) * Main.rand.NextFloat(ArmSpeedMin, ArmSpeedMax) * boost;
                    Color col = Color.Lerp(RequiemVfx.Gold, RequiemVfx.GoldWhite, Main.rand.NextFloat());
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(c, v, col, Main.rand.NextFloat(RequiemVfx.SparkScaleMin, RequiemVfx.SparkScaleMax)).Configure(false, ArmSparkLife, RequiemVfx.SparkSquash, true);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = CEExtraAssets.Glow2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float alpha = Projectile.Opacity;
            Color soft = RequiemVfx.Gold * (BloomOpacity * alpha);
            soft.A = 0;
            Main.EntitySpriteDraw(bloom, pos, null, soft, 0f, bloom.Size() / 2f, BloomScale * Projectile.scale, SpriteEffects.None);
            Color halo = RequiemVfx.GoldWhite * (HaloOpacity * alpha);
            halo.A = 0;
            Main.EntitySpriteDraw(tex, pos, null, halo, 0f, CrossOrigin, Projectile.scale * HaloScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, Color.White * alpha, 0f, CrossOrigin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
