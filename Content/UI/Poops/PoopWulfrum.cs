using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopWulfrum : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopWulfrumProjectile>();
        }
        public override float getRollChance() {
            return 0.2f;
        }
    }
}
