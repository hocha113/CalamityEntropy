using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopWhite : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopWhiteProjectile>();
        }
        public override float getRollChance() {
            return 0.2f;
        }
    }
}
