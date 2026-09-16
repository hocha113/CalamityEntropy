using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopGiant : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopGiantProjectile>();
        }

        public override float getRollChance() {
            return 0.1f;
        }
    }
}
