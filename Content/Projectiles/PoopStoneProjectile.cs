using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopStoneProjectile : PoopProj
    {
        public override bool BreakWhenHitNPC => false;
        public override int damageChance => 35;
        public override void OnSpawn(IEntitySource source) {
            base.OnSpawn(source);
            Projectile.damage *= 3;
        }
        public override int dustType => DustID.Stone;
    }

}