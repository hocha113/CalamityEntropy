using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class SpImpact : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 20;
            Projectile.penetrate = -1;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
        }


        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D tx = CEExtraAssets.impact;
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White * ((float)Projectile.timeLeft / 20f), Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, ((float)(40 - Projectile.timeLeft)) / 160f, SpriteEffects.None, 0);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }


}