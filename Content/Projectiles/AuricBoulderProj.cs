using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AuricBoulderProj : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 7;
        }
        public override void SetDefaults() {
            base.Projectile.width = 32;
            base.Projectile.height = 32;
            base.Projectile.scale = 1f;
            base.Projectile.alpha = 0;
            base.Projectile.timeLeft = 3600;
            base.Projectile.penetrate = -1;
            base.Projectile.aiStyle = 25;
            base.AIType = 1013;
            base.Projectile.tileCollide = true;
            base.Projectile.friendly = true;
            base.Projectile.hostile = true;
            base.Projectile.ignoreWater = true;
            base.Projectile.extraUpdates = 0;
            base.Projectile.usesLocalNPCImmunity = true;
            base.Projectile.localNPCHitCooldown = 10;
        }

        public override void AI() {
            if (base.Projectile.velocity.X >= 0f) {
                base.Projectile.rotation += 0.2f;
            }
            else if (base.Projectile.velocity.X <= 0f) {
                base.Projectile.rotation -= 0.2f;
            }
            if (Main.GameUpdateCount % 3 == 0) {
                Projectile.frame++;
                if (Projectile.frame >= 7) {
                    Projectile.frame = 0;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (Projectile.velocity.X == 0 && oldVelocity.X != 0) {
                Projectile.velocity.X = -oldVelocity.X * 2.5f;
                if (Projectile.velocity.Length() > 3) {
                    SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/shockBlast"));
                }
            }
            if (Projectile.velocity.Y == 0 && oldVelocity.Y != 0) {
                Projectile.velocity.Y = -oldVelocity.Y * 2.5f;
                if (Projectile.velocity.Length() > 3) {
                    SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/shockBlast"));
                }
            }
            if (Projectile.timeLeft < 2800) {
                Projectile.timeLeft = 0;
                for (int i = 0; i < 16; i++) {
                    Dust.NewDust(Projectile.Center, 32, 32, DustID.Pixie, 0, 0);
                    SoundEngine.PlaySound(SoundID.Tink with { Pitch = 0.3f, PitchVariance = 0.25f }, Projectile.Center);
                }
            }
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.velocity = (Projectile.velocity.SafeNormalize(Vector2.Zero) + (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero)).SafeNormalize(Vector2.Zero) * 32;
            SoundEngine.PlaySound(SoundID.Tink with { Pitch = 0.3f, PitchVariance = 0.25f }, Projectile.Center);

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.velocity = (Projectile.velocity.SafeNormalize(Vector2.Zero) + (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero)).SafeNormalize(Vector2.Zero) * 32;
            SoundEngine.PlaySound(SoundID.Tink with { Pitch = 0.3f, PitchVariance = 0.25f }, Projectile.Center);

        }
    }
}
