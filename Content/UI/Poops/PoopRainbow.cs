using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopRainbow : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopRainbowProjectile>();
        }

        public override float getRollChance() {
            return 0.1f;
        }
    }
}
