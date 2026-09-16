using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Detector : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        bool back = false;
        public int ShootTimes = 6;
        public int ShootDelay = 0;
        public override void AI() {
            Projectile.pushByOther(1.4f);
            if (back) {
                Projectile.velocity *= 0.86f;
                Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * 6;
                if (CEUtils.getDistance(Projectile.Center, Projectile.GetOwner().Center) < Projectile.velocity.Length() * 1.2f) {
                    Projectile.Kill();
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else {
                NPC target = Projectile.FindTargetWithinRange(3000);
                ShootDelay--;
                if (target != null) {
                    if (CEUtils.getDistance(Projectile.Center, target.Center) > 400) {
                        Projectile.velocity *= 0.99f;
                        Projectile.velocity += (target.Center - Projectile.Center).normalize() * 0.7f;
                    }
                    if (CEUtils.getDistance(Projectile.Center, target.Center) < 800 && ShootDelay <= 0) {
                        if (Main.myPlayer == Projectile.owner) {
                            int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (target.Center - Projectile.Center).normalize() * 12, 88, Projectile.damage, Projectile.knockBack, Projectile.owner);
                            p.ToProj().usesLocalNPCImmunity = true;
                            p.ToProj().localNPCHitCooldown = 16;
                        }
                        ShootDelay = 25;
                        SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);
                        if (ShootTimes-- == 0) {
                            back = true;
                        }
                    }
                    Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
                }
                else {
                    if (Projectile.GetOwner().Distance(Projectile.Center) > 120) {
                        Projectile.velocity *= 0.99f;
                        Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * 0.6f;
                        Projectile.rotation = Projectile.velocity.ToRotation();
                    }
                }
            }
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
    }


}