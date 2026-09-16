using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopAuric : Poop
    {
        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopAuricProjectile>();
        }

        public override float getRollChance() {
            return 0.08f;
        }
    }
}
