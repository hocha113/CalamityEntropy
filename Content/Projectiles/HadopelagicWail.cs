using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class HadopelagicWail : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 230;
            Projectile.height = 230;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.extraUpdates = 1;
            Projectile.ArmorPenetration = 36;
        }

        public override void AI() {
            if (Projectile.ai[0] == 0) {
                //HadLine带rotation的Configure对齐旧HadLine构造
                PRTLoader.NewParticle<PRT_HadLine>(Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * 120, Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation());
                PRTLoader.NewParticle<PRT_HadLine>(Projectile.Center + Projectile.velocity.RotatedBy(-MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * 120, Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation());

            }
            Projectile.ai[0]++;
            PRTLoader.NewParticle<PRT_HadCircle>(Projectile.Center, Vector2.Zero, Color.White, 0.6f).Configure(0, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation());

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<LifeOppress>(600);
            //AdditiveBlend走Configure分桶,旧版粒子系统 Before层那套
            PRTLoader.NewParticle<PRT_HadCircle2>(target.Center, Vector2.Zero, new Color(170, 170, 255), 0).Configure(0, true, PRTDrawModeEnum.AdditiveBlend, 0);
            PRTLoader.NewParticle<PRT_HadCircle2>(target.Center, Vector2.Zero, new Color(170, 170, 255), 0).Configure(0, true, PRTDrawModeEnum.AdditiveBlend, 0);
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }


}