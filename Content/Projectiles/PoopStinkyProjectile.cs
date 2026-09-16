using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopStinkyProjectile : PoopProj
    {
        public int spawnCd = 0;
        public override void AI() {
            base.AI();
            spawnCd--;
            if (spawnCd <= 0) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FartCloud>(), Projectile.damage / 14, 0, Projectile.owner);
                spawnCd = 30;
            }
        }

        public override void OnKill(int timeLeft) {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FartCloud>(), Projectile.damage / 14, 0, Projectile.owner);

        }
    }

}