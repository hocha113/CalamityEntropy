using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopRed : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopRedProjectile>();
        }
    }
}
