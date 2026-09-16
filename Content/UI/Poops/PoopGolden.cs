using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopGolden : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopGoldenProjectile>();
        }
        public override float getRollChance() {
            return 0.32f;
        }
    }
}
