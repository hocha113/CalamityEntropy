using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RbCircle : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.damage = 0;
            Projectile.DamageType = DamageClass.Default;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 2;
        }
        public override void AI() {
            Player player = Projectile.owner.ToPlayer();

            if (player.Entropy().reincarnationBadge) {
                Projectile.timeLeft = 3;
            }
            Projectile.Center = player.Center + new Vector2(0, -80) + player.gfxOffY * Vector2.UnitY;
            if (player.Entropy().rBadgeCharge >= 12 && !player.Entropy().rBadgeActive) {
                if (alpha > 0) {
                    alpha -= 0.05f;
                }
            }
            else {
                if (alpha < 1) {
                    alpha += 0.05f;
                }
            }
            if (player.Entropy().rBadgeActive) {
                foreach (Projectile p in Main.projectile) {
                    if (p.active && CEUtils.getDistance(p.Center, player.Center) < 360 * Projectile.scale * 1.6f && p.hostile && p.velocity.Length() > 0.02f) {
                        p.velocity += (p.Center - player.Center).SafeNormalize(new Vector2(1, 0)) * p.velocity.Length() * 0.05f * ((360 * Projectile.scale * 1.6f - CEUtils.getDistance(p.Center, player.Center)) / 360 * Projectile.scale * 1.6f);
                        Dust.NewDust(p.Center - new Vector2(3, 3), 6, 6, DustID.YellowStarDust, 0, 0);
                    }
                }
            }
        }
        public override bool? CanCutTiles() {
            return false;
        }
        float alpha = 1;
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D t1 = CEUtils.getExtraTex("sCircle");
            Texture2D t2 = CEUtils.getExtraTex("sDot");
            Color color = new Color(Main.rand.Next(0, 256), Main.rand.Next(0, 256), Main.rand.Next(0, 256));
            if (Projectile.owner.ToPlayer().Entropy().rBadgeActive) {
                Main.spriteBatch.Draw(t1, Projectile.Center - Main.screenPosition, null, color, 0, t1.Size() / 2, Projectile.scale * 0.25f, SpriteEffects.None, 0);
            }
            float counts = Projectile.owner.ToPlayer().Entropy().rBadgeCharge;
            int dotCount = (int)(Math.Ceiling(counts));
            float rot = MathHelper.ToRadians(-30);
            for (int i = 0; i < dotCount; i++) {
                float scale = 1;
                if (i == dotCount - 1 && !(i == 11 && counts >= 12)) {
                    scale = counts - (int)counts;
                }
                Vector2 drawPos = Projectile.Center + rot.ToRotationVector2() * (14 + 16 * Projectile.owner.ToPlayer().Entropy().rbDotDist);
                Main.spriteBatch.Draw(t2, drawPos - Main.screenPosition, null, new Color(170, 240, 90) * alpha, 0, t2.Size() / 2, Projectile.scale * 0.2f * scale, SpriteEffects.None, 0);
                rot += MathHelper.ToRadians(360 / 12);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
    }

}