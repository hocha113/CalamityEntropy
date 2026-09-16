using CalamityEntropy.Content.Buffs.PortsDoT;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AstralShot : ModProjectile
    {

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }
        public bool playsound = true;
        public override void AI() {
            if (playsound) {
                playsound = false;
                SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("CalamityEntropy/Assets/Sounds/lasershoot") { Volume = 0.35f }, Projectile.Center);
            }
            Projectile.extraUpdates = 2;
            float maxVelocity = 10f;
            if (Projectile.velocity.Length() < maxVelocity) {
                Projectile.velocity *= 1.03f;
                if (Projectile.velocity.Length() > maxVelocity) {
                    Projectile.velocity.Normalize();
                    Projectile.velocity *= maxVelocity;
                }
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;

            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 45);
        }

        public override Color? GetAlpha(Color lightColor) {
            return new Color(255, 255, 255, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor) {
            CEUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
