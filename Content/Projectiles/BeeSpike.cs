using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class BeeSpike : ModProjectile
    {

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 20;
            Projectile.tileCollide = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
            Projectile.penetrate = 3;
        }
        public override void AI() {
            Projectile.Opacity -= 1f / 20f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.timeLeft < 6) {
                return false;
            }
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Poisoned, 6 * 60);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, new Vector2(18, 9), Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}