using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopWhiteProjectile : PoopProj
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.light = 1f;
        }
        public override void AI() {
            base.AI();
            foreach (Player p in Main.ActivePlayers) {
                if (CEUtils.getDistance(Projectile.Center, p.Center) < 64 * Projectile.scale * 2f) {
                    p.Entropy().holyGroundTime = 2;
                }
            }
        }
        public override void PostDraw(Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix); ;

            Texture2D light = CEUtils.getExtraTex("godhead");
            Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition, null, Color.White * 0.3f, 0, light.Size() / 2, 4 * Projectile.scale, SpriteEffects.None, 0); ;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix); ;

        }

        public override int dustType => DustID.HallowedTorch;
    }

}