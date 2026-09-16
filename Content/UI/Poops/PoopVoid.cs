using CalamityEntropy.Content.Projectiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public class PoopVoid : Poop
    {

        public override int ProjectileType() {
            return ModContent.ProjectileType<PoopVoidProjectile>();
        }
        public override float getRollChance() {
            return 0.15f;
        }
    }
}
