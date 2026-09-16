using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopCornProjectile : PoopProj
    {
        public int spawnCd = 0;
        public int maxSpawn = 3;
        public override void AI() {
            base.AI();
            int h = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile is BlueFlies && p.ai[0] == Projectile.whoAmI) {
                    h++;
                }
            }
            if (spawnCd > 0) {
                spawnCd--;
            }
            if (h < maxSpawn && Main.myPlayer == Projectile.owner) {
                if (spawnCd <= 0) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BlueFlies>(), Projectile.damage / 2, 2, Projectile.owner, Projectile.whoAmI);
                    spawnCd = 160;
                }
            }
        }
    }

}