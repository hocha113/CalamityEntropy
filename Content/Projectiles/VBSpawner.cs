using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VBSpawner : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 10;
        }
        public int spawned = 0;
        public int getMax() {
            return 6 + Projectile.owner.ToPlayer().Entropy().WeaponBoost;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public float rot = 0;
        public bool setRot = true;
        public override void AI() {
            if (setRot) {
                setRot = false;
                rot = (float)((Main.rand.NextDouble() - 0.5) * Math.PI * 2);
            }
            Projectile.Center = Projectile.owner.ToPlayer().Center;
            if (Projectile.owner.ToPlayer().channel) {
                Projectile.timeLeft = 3;
            }
            if (Projectile.ai[0] % 30 == 0 && spawned < getMax() && Projectile.owner == Main.myPlayer) {
                Vector2 offset = Projectile.Center - Projectile.owner.ToPlayer().Center + new Vector2(200, 0).RotatedBy(rot);
                int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer().Center + offset, Vector2.Zero, ModContent.ProjectileType<VoidBlaster>(), Projectile.damage, 0, Projectile.owner, 0, offset.X, offset.Y);
                spawned++;
                p.ToProj().scale = Projectile.scale;
                rot += (float)(Math.PI * 2 / getMax());
            }
            Projectile.owner.ToPlayer().itemAnimation = 2;
            Projectile.owner.ToPlayer().itemTime = 2;
            Projectile.ai[0]++;

        }


        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }


}