using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopStone : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopStoneProjectile>();
        }
    }
}
