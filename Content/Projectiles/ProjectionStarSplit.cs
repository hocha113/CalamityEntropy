using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ProjectionStarSplit : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 2;
        }
        public override void AI() {
            Projectile.rotation += 0.16f;
            NPC target = Projectile.FindTargetWithinRange(2000, false);
            if (target != null) {
                Projectile.velocity *= 0.95f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();

                Projectile.velocity += v * 1f;
            }
            Dust.NewDust(Projectile.Center - new Vector2(3, 3), 6, 6, DustID.YellowStarDust, 0, 0);
        }


        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.timeLeft < 30) {
                lightColor *= ((float)Projectile.timeLeft / 30f);
            }
            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            CEUtils.DrawAfterimage(tx, Projectile.Entropy().odp, Projectile.Entropy().odr);
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tx.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }


}