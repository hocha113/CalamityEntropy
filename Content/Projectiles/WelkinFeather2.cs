using CalamityEntropy.Content.Items.Books;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class WelkinFeather2 : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 4;
            Projectile.tileCollide = false;
            Projectile.light = 0.16f;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 1;
        }
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D t = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, t.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }

}