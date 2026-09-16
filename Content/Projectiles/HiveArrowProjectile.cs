using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class HiveArrowProjectile : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.MaxUpdates = 3;
            Projectile.arrow = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 1200;
            Projectile.tileCollide = true;
        }
        public override void AI() {
            if (++Projectile.ai[0] > 6) {
                if (homingSpeed < 2) {
                    homingSpeed += 0.003f;
                }
                NPC homing = Projectile.FindTargetWithinRange(900, true);
                if (homing != null) {
                    Projectile.velocity = (CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (homing.Center - Projectile.Center).ToRotation(), 1 * homingSpeed.ToRadians())).ToRotationVector2() * Projectile.velocity.Length();
                    Projectile.velocity = (CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (homing.Center - Projectile.Center).ToRotation(), 0.12f * homingSpeed, false)).ToRotationVector2() * Projectile.velocity.Length();
                }
                else {
                    Projectile.velocity.Y += homingSpeed * 0.2f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
                if (Projectile.frame > 3) {
                    Projectile.frame = 0;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Poisoned, 3 * 60);
        }
        public float homingSpeed = 0;
        public override void OnKill(int timeLeft) {
            if (!Main.dedServ) {
                CEUtils.PlaySound("beeSting", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, volume: 0.7f);
            }
            if (Main.myPlayer == Projectile.owner) {
                for (int i = 0; i < 7; i++) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(10, 15), ModContent.ProjectileType<BeeSpike>(), Projectile.damage / 4, Projectile.knockBack / 2, Projectile.owner);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(this.getTextureAlt(), Projectile.Center - Main.screenPosition, new Rectangle(0, 42 * Projectile.frame, 22, 40), lightColor, Projectile.rotation, new Vector2(11, 40), Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
