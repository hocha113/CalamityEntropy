using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopStinky : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopStinkyProjectile>();
        }
    }
}
