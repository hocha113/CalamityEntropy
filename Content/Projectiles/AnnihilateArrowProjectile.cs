using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AnnihilateArrowProjectile : ModProjectile
    {

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.MaxUpdates = 5;
            Projectile.arrow = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 1200;
            Projectile.light = 0.2f;
            Projectile.tileCollide = false;
        }

        public override void AI() {
            if (homing && Projectile.ai[0]++ > 10) {
                NPC target = Projectile.FindTargetWithinRange(400);
                if (target != null) {
                    float rot = CEUtils.randomRot();
                    for (int i = 0; i < 4; i++) {
                        float a = rot + MathHelper.ToRadians(i * 90);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * Projectile.velocity.Length() * 0.36f, ModContent.ProjectileType<AnnihilateArrowSplit>(), Projectile.damage / 3, Projectile.knockBack / 4, Projectile.owner);
                    }
                    Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((target.Center - Projectile.Center).ToRotation());
                    homing = false;
                    for (int i = 0; i < 6; i++) {
                        Vector2 top = Projectile.Center;
                        Vector2 sparkVelocity2 = Projectile.velocity.RotateRandom(0.26f) * -1 * Main.rand.NextFloat(1, 5);
                        int sparkLifetime2 = Main.rand.Next(18, 22);
                        float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                        Color sparkColor2 = Color.Lerp(Color.Blue, Color.DeepSkyBlue, Main.rand.NextFloat(0, 1));
                        //PRT_LineCal Configure(false,lifetime)对齐Calamity LineParticle
                        PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                    }
                    if (!Main.dedServ) {
                        for (int i = 0; i < 16; i++) {
                            PRTLoader.NewParticle<PRT_Pixel>(Vector2.Zero, Vector2.Zero, Color.White, 1f)
                                .Configure(Projectile.Center, Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(100, 256), Projectile.Center, Main.rand.Next(20, 42), Color.White, new Color(180, 180, 255));
                        }
                    }
                }
            }
            if (Projectile.localAI[2]++ > 2 && Main.rand.NextBool(2)) {
                Vector2 pos = Projectile.Center - Projectile.velocity.normalize() * 9 + CEUtils.randomPointInCircle(2);
                Vector2 vel = Projectile.velocity.normalize();
                Color clr = new Color(Main.rand.Next(40, 100), Main.rand.Next(40, 100), 255);
                float scale = Main.rand.NextFloat(0.7f, 1) * 0.05f;
                //GlowSparkCal Configure里stretch/glow是Calamity原参,别当EParticle尾参
                PRTLoader.NewParticle<PRT_GlowSparkCal>(pos, vel, clr, scale).Configure(false, 8, new Vector2(0.2f, 1));
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public bool homing = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("bne_hit", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, volume: 0.4f);
            var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(target.Center, Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
            __prt.lx = 1.9f;
            __prt.xadd = 1.9f;

            if (homing) {
                float rot = CEUtils.randomRot();
                for (int i = 0; i < 3; i++) {
                    float a = rot + MathHelper.ToRadians(i * 120);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * Projectile.velocity.Length() * 0.5f, ModContent.ProjectileType<AnnihilateArrowSplit>(), Projectile.damage / 4, Projectile.knockBack / 4, Projectile.owner);
                }
                int pjex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 270 * Projectile.WhipSettings.RangeMultiplier, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), 0, 0, Projectile.owner, 0, -0.8f);
                pjex.ToProj().hostile = false;
                pjex.ToProj().MaxUpdates *= 2;
            }
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("bne_hit2", Main.rand.NextFloat(0.7f, 1.3f), Projectile.Center);
        }
    }
}