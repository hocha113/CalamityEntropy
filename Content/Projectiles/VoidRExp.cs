using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VoidRExp : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.width = 256;
            Projectile.height = 256;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 90;

        }

        public override void AI() {
            Projectile.extraUpdates = (int)Projectile.ai[1];
            int n = (int)Projectile.ai[1] - 1;
            if (n >= 0) {
                if (n.ToNPC().active) {
                    Projectile.Center = n.ToNPC().Center;
                }
                else {
                    Projectile.ai[1] = 0;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) {

            return false;
        }
    }

}