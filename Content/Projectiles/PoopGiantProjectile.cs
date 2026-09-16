using Terraria;
using Terraria.DataStructures;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopGiantProjectile : PoopProj
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.width = Projectile.height = 64;

        }
        public override void OnSpawn(IEntitySource source) {
            base.OnSpawn(source);
            Projectile.damage *= 3;
        }
        public override bool BreakWhenHitNPC => false;

        public override int damageChance => 18;
    }

}