using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RedemptionSpear : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 1;
            Projectile.ArmorPenetration = 15;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            hitbox = Projectile.Center.getRectCentered(52 * Projectile.scale, 52 * Projectile.scale);
        }
        public PRT_TrailParticle trail = null;
        public override void AI() {
            base.AI();
            if (trail == null) {
                //TrailParticle不开CanPool,odp轨迹List池化会闪上一条
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.Yellow, 2).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                trail.maxLength = 8;
                trail.SameAlpha = true;
                trail.ShouldDraw = false;
            }
            trail.AddPoint(Projectile.Center + Projectile.velocity + Projectile.velocity.normalize() * 12);
            trail.Lifetime = 12;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Vector2 top = Projectile.Center;
            Vector2 sparkVelocity2 = Projectile.velocity * -0.03f;
            int sparkLifetime2 = 6;
            float sparkScale2 = Main.rand.NextFloat(1f, 1.2f);
            Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.Gold, Main.rand.NextFloat(0, 1));
            PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            CEUtils.AddLight(Projectile.Center, Color.LightGoldenrodYellow);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseAdditive();
            if (trail != null)
                trail.DrawTrail(Main.spriteBatch);
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver2) {
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + i.ToRotationVector2() * 4, null, this.color, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + i.ToRotationVector2() * 4, null, this.color, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, this.color, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            CEUtils.DrawGlow(Projectile.Center, new Color(this.color.R * 0.4f, this.color.G * 0.36f, this.color.B * 0.05f) * 0.3f, Projectile.scale * 4);
            return false;
        }
        public override Color baseColor => Color.White;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            //StrikeParticle redemption spear命中层
            PRTLoader.NewParticle<PRT_StrikeParticle>(Projectile.Center - Projectile.velocity * 7, Projectile.velocity * 3, color, Projectile.scale * 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation());
            for (int i = 0; i < 24; i++) {
                // 原灾厄SquashDust光珠,用自有GlowOrbCal等效(带重力,金色)
                Vector2 vel = (new Vector2(35, 35).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                PRTLoader.NewParticle<PRT_GlowOrbCal>(Projectile.Center, vel, Color.Goldenrod, Main.rand.NextFloat(2f, 2.5f)).Configure(true, 25);
            }
            SoundEngine.PlaySound(SoundID.Item96 with { Pitch = 0.6f, Volume = 1f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
            PRTLoader.NewParticle<PRT_RedemptionSpearParticle>(Projectile.Center, Projectile.velocity, Color.White, Projectile.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, Projectile.rotation);
        }
    }
}