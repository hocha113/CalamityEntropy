using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class GoozmaStarShot : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 60;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 20;
            Projectile.light = 1;
            Projectile.extraUpdates = 1;
        }
        public override void AI() {
            base.AI();
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 2) {
                if (this.ShooterModProjectile is EntropyBookHeldProjectile mp) {
                    NPC target = Projectile.FindTargetWithinRange(2400);
                    mp.ShootSingleProjectile(mp.getShootProjectileType(), Projectile.Center, (target == null ? Projectile.velocity : (target.Center - Projectile.Center)), 1);
                }
            }

            //HeavySmokeCal Configure是Calamity原构造顺序,跟EParticle统一尾参不是一回事
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.rotation.ToRotationVector2() * -20 + Projectile.velocity, Main.DiscoColor, Main.rand.NextFloat(0.3f, 0.5f)).Configure(1f, 18, 0.012f, true, 0.01f, true);

            Projectile.velocity *= 0.95f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
            float r = CEUtils.randomRot();
            //AbyssalLine带lifetime的Configure是CalamityPorts签名
            var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, Color.LightBlue, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, r);
            __prt.lx = 1.4f;
            __prt.xadd = 0.6f;
            var __prt2 = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, Color.LightBlue, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, r + MathHelper.PiOver2);
            __prt2.lx = 1.4f;
            __prt2.xadd = 0.6f;
            for (int i = 0; i < Main.rand.Next(1, 3); i++) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomVec(24), ModContent.ProjectileType<GRainbowRocket>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomVec(24), ModContent.ProjectileType<PartySparkle>(), (int)(Projectile.damage * 0.8f), Projectile.knockBack, Projectile.owner);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = CEExtraAssets.StarTexture;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Main.DiscoColor, 4 * Main.GlobalTimeWrappedHourly, tex.Size() / 2, Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Main.DiscoColor, -1 * Main.GlobalTimeWrappedHourly, tex.Size() / 2, Projectile.scale * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Main.DiscoColor, -2 * Main.GlobalTimeWrappedHourly, tex.Size() / 2, Projectile.scale * 0.44f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
            return false;
        }
    }

    /// <summary>
    /// 派对星光。灾厄 PartySparkle 的自有移植(同短名), 供 GoozmaStarShot 与 GRainbowRocket 使用。
    /// </summary>
    public class PartySparkle : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Particles/StarProj";

        //派对彩虹色盘, 原灾厄 RainbowPartyCannon.ColorSet 数值内联
        public static readonly Color[] ColorSet = new Color[]
        {
            new Color(188, 192, 193),
            new Color(157, 100, 183),
            new Color(249, 166, 77),
            new Color(255, 105, 234),
            new Color(67, 204, 219),
            new Color(249, 245, 99),
            new Color(236, 168, 247),
        };

        public float Time {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public float ColorSpectrumHue {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }
        public const int Lifetime = 90;
        public const int FadeinTime = 18;
        public const int FadeoutTime = 18;

        public override void SetDefaults() {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.scale = 0.001f;
        }

        public override void AI() {
            if (Time == 1f) {
                Projectile.scale = Main.rand.NextFloat(0.4f, 1.1f);
                int size = (int)(72 * Projectile.scale);
                Projectile.Resize(size, size);
                ColorSpectrumHue = Main.rand.NextFloat(0f, 0.9999f);
                Projectile.netUpdate = true;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            Time++;

            Projectile.velocity *= 0.96f;

            Projectile.rotation = Projectile.rotation.AngleLerp(MathHelper.PiOver2, 0.085f);

            //一生内跨越色盘 33%, 让星光颜色缓慢流动
            ColorSpectrumHue = (ColorSpectrumHue + 0.333f / Lifetime) % 0.999f;

            Projectile.Opacity = Utils.GetLerpValue(0f, FadeinTime, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - FadeoutTime, Time, true);
            Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin(Time / 30f) * 0.0125f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D sparkleTexture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;

            Color sparkleColor = CEUtils.MulticolorLerp(ColorSpectrumHue, ColorSet) * Projectile.Opacity * 0.5f;
            sparkleColor.A = 0;

            sparkleColor *= MathHelper.Lerp(1f, 1.5f, Utils.GetLerpValue(Lifetime * 0.5f - 15f, Lifetime * 0.5f + 15f, Time, true));

            Color orthogonalsparkleColor = Color.Lerp(sparkleColor, Color.White, 0.5f) * 0.5f;

            Vector2 origin = sparkleTexture.Size() / 2f;

            Vector2 sparkleScale = new Vector2(0.3f, 1f) * Projectile.Opacity * Projectile.scale;
            Vector2 orthogonalsparkleScale = new Vector2(0.3f, 2f) * Projectile.Opacity * Projectile.scale;

            Main.EntitySpriteDraw(sparkleTexture, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY, null, sparkleColor, MathHelper.PiOver2 + Projectile.rotation, origin, orthogonalsparkleScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(sparkleTexture, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY, null, sparkleColor, Projectile.rotation, origin, sparkleScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(sparkleTexture, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY, null, orthogonalsparkleColor, MathHelper.PiOver2 + Projectile.rotation, origin, orthogonalsparkleScale * 0.6f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(sparkleTexture, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY, null, orthogonalsparkleColor, Projectile.rotation, origin, sparkleScale * 0.6f, SpriteEffects.None, 0);
            return false;
        }
    }


}