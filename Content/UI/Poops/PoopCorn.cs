using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopCorn : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopCornProjectile>();
        }
    }
}
