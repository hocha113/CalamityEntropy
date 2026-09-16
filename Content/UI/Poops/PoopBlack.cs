using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopBlack : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopBlackProjectile>();
        }
    }
}
