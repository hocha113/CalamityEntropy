using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class SmallBee : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.MaxUpdates = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 1200;
            Projectile.tileCollide = true;
            Projectile.ArmorPenetration = 46;
        }
        public override void AI() {
            if (++Projectile.ai[0] > 6) {
                if (homingSpeed < 2) {
                    homingSpeed += 0.003f;
                }
                NPC homing = Projectile.FindTargetWithinRange(1600, true);
                if (homing != null) {
                    Projectile.velocity += (homing.Center - Projectile.Center).normalize() * 0.3f;
                    Projectile.velocity *= 0.98f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
                if (Projectile.frame > 1) {
                    Projectile.frame = 0;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (Projectile.GetOwner().strongBees)
                modifiers.FinalDamage *= 1.4f;
        }
        public override bool? CanHitNPC(NPC target) {
            return Projectile.ai[0] > 12 ? null : false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Poisoned, 8 * 60);
        }
        public float homingSpeed = 0;
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, new Rectangle(0, 14 * Projectile.frame, 14, 14), lightColor, Projectile.rotation, new Vector2(7, 6), Projectile.scale * (Projectile.GetOwner().strongBees ? 1.5f : 1), SpriteEffects.None);
            return false;
        }
    }
}