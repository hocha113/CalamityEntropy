using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ALMGShell : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.timeLeft = 160;
            Projectile.penetrate = -1;
        }
        public override void AI() {
            Projectile.velocity.X *= 0.98f;

            Projectile.velocity.Y += 0.6f;
            Projectile.rotation += Projectile.velocity.X * 0.06f;
            Projectile.Opacity = Projectile.timeLeft > 60 ? 1 : (float)Projectile.timeLeft / 60f;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (oldVelocity.Y != 0 && Projectile.velocity.Y == 0) {
                Projectile.velocity.Y = oldVelocity.Y * -0.6f;
                Projectile.velocity.X *= 0.8f;
            }
            return false;
        }
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            fallThrough = false;
            return true;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }

    }


}