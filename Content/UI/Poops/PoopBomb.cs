using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopBomb : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopBombProjectile>();
        }
    }
}
