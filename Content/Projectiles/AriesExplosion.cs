using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class AriesExplosion : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 126;
        }
        public override void AI() {
            if (Projectile.ai[0] == 0) {
                CEUtils.PlaySound("explosion", 1, Projectile.Center, 4);
                //PRT_PlasmaExplosionCal Aries爆炸,Calamity plasma explode原参
                PRTLoader.NewParticle<PRT_PlasmaExplosionCal>(Projectile.Center, Vector2.Zero, new Color(160, 120, 255), 0f).Configure(new Vector2(2f, 2f), 0, 0.032f, 46);
                PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, new Color(180, 156, 255), 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 0.4f, 30);
                for (int i = 0; i < 28; i++) {
                    //Smoke vd/ad字段spawn后赋,旧EParticle Smoke初始化器
                    var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6, 16) * 0.2f, new Color(140, 140, 255), 0.06f);
                    p.Lifetime = 26;
                    p.timeleftmax = 26;
                    p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0f, 26);
                    p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6, 16) * 0.2f, Color.LightGoldenrodYellow, 0.06f);
                    p.Lifetime = 26;
                    p.timeleftmax = 26;
                    p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0f, 26);
                }
            }
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }

}