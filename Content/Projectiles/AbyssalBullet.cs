using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AbyssalBullet : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 400;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }
        NPC homing = null;
        public override void AI() {
            Projectile.localAI[0]++;
            if (homing == null) {
                homing = Projectile.FindTargetWithinRange(1200);
            }
            else {
                if (!homing.active) {
                    homing = null;
                }
            }
            if (homing != null && Projectile.localAI[0] > 19) {
                Projectile.velocity += (homing.Center - Projectile.Center).normalize() * 0.5f;
                Projectile.velocity *= 0.97f;
            }
            if (Projectile.timeLeft < 40) {
                Projectile.Opacity -= 1 / 40f;
            }
            for (int i = 0; i < 5; i++) {
                //PRT_Abyssal不进常规PRT桶,EffectLoader DrawParticleEffectsAlt RT画
                var p = PRTLoader.NewParticle<PRT_Abyssal>(Projectile.Center, CEUtils.randomPointInCircle(3), Color.White, 1f);
                p.vd = 0.96f;
                p.ad = 0.05f;
                p.Opacity = 0.38f * Main.rand.NextFloat(0.8f, 1);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 300);
            for (int i = 0; i < 16; i++) {
                //vd/ad直赋对齐旧AbyssalParticles,Configure签名跟EParticle系不一样
                var p = PRTLoader.NewParticle<PRT_Abyssal>(Projectile.Center, CEUtils.randomPointInCircle(6), Color.White, 1f);
                p.Opacity = 0.6f * Main.rand.NextFloat(0.4f, 1);
                p.vd = 0.98f;
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }


}