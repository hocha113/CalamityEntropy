using CalamityEntropy.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopVoidProjectile : PoopProj
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.light = 1f;
        }
        public override bool BreakWhenHitNPC => false;
        public override int damageChance => 12;
        public override void OnSpawn(IEntitySource source) {
            base.OnSpawn(source);
            Projectile.damage *= 5;
        }
        public override void AI() {
            base.AI();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.hostile && p.velocity != Vector2.Zero && CEUtils.getDistance(p.Center, Projectile.Center) < 640) {
                    p.velocity += (Projectile.Center - p.Center).SafeNormalize(Vector2.Zero) * 1f;
                }
            }
            foreach (NPC p in Main.ActiveNPCs) {
                if (!p.friendly && p.velocity != Vector2.Zero && CEUtils.getDistance(p.Center, Projectile.Center) < 640) {
                    p.velocity += (Projectile.Center - p.Center).SafeNormalize(Vector2.Zero) * 0.4f;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 600, 40);
        }
        public override int dustType => DustID.Corruption;
    }

}