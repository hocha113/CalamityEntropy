using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VoidMonsterShoot : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 400;
            Projectile.extraUpdates = 1;
            Projectile.ArmorPenetration = 36;
        }
        public float ap = 0;
        public override void AI() {
            for (int i = 0; i < 10; i++) {
                //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.velocity * (i * 0.1f), Vector2.Zero, Color.White, 1f);
                p.Opacity = 0.14f;  //Opacity旧初始化器字段,Configure管不了
            }

            NPC target = Projectile.FindTargetWithinRange(900, false);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (target != null) {
                Projectile.velocity *= 0.9f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();

                Projectile.velocity += v * 2.6f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 30, 1, 120, 16);
        }

        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }

}
