using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FractalBeam : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 500;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return null;
        }
        public override void CutTiles() {
        }
        public override void AI() {
            Projectile.ai[0]++;
            if (Projectile.ai[0] > 20)
                Projectile.HomingToNPCNearby(1.4f, 0.665f, 1200);
            else
                Projectile.velocity *= 0.95f;
            if (Projectile.timeLeft < 30)
                Projectile.Opacity -= 1 / 30f;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D s = CEExtraAssets.StarTexture;
            Main.spriteBatch.UseAdditive();

            float alpha = Projectile.Opacity;
            float scale = 0.5f + 0.12f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 32));
            Color color = Projectile.whoAmI.GetHashCode() % 2 == 0 ? Color.SkyBlue : Color.LimeGreen * 3;
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, color * alpha, 0, s.Size() * 0.5f, new Vector2(1, 0.2f) * Projectile.scale * scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, color * alpha, 0, s.Size() * 0.5f, new Vector2(0.2f, 1) * Projectile.scale * scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, color * alpha, 0, s.Size() * 0.5f, new Vector2(0.6f, 0.12f) * Projectile.scale * scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, color * alpha, 0, s.Size() * 0.5f, new Vector2(0.6f, 0.12f) * Projectile.scale * scale, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}