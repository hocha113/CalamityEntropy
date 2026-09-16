using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopFlamingProjectile : PoopProj
    {
        public override void OnSpawn(IEntitySource source) {
            base.OnSpawn(source);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Flame>(), Projectile.damage * 2, 2, Projectile.owner, 0, Projectile.identity);
        }
    }
}