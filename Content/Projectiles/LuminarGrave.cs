using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LuminarGrave : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = 26;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.timeLeft = 800;
            Projectile.penetrate = -1;
        }
        public override void AI() {
            if (Projectile.velocity.Length() > 0.2f) {
                Projectile.velocity.X *= 0.98f;
                Projectile.rotation += Projectile.velocity.X * 0.06f;
            }
            else {
                Projectile.rotation = 0;
            }
            Projectile.velocity.Y += 0.52f;
            Projectile.Opacity = Projectile.timeLeft > 60 ? 1 : (float)Projectile.timeLeft / 60f;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.velocity *= 0;
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