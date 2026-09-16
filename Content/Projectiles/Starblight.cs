using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Starblight : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 12;
        }
        public int counter = 0;
        public bool std = false;
        public int homingTime = 60;
        public PRT_StarTrailParticle spt = null;
        public override void AI() {
            if (spt == null) {
                //StarTrailParticle星尘拖尾,旧EParticle StarTrail
                spt = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.Zero, Color.LightBlue * 1.5f, 1.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
            spt.maxLength = 16;
            spt.Lifetime = 30;
            counter++;
            Projectile.ai[0]++;

            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1400);
            if (target != null && CEUtils.getDistance(target.Center, Projectile.Center) < 200 && counter > 6) {
                homingTime = 0;
                Projectile.velocity *= 0.9f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();
                Projectile.velocity += v * 3f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();


            if (Projectile.velocity.Length() > 3) {
                Projectile.velocity *= 0.995f - homing * 0.018f;
            }
            if (counter > 2) {
                if (homing < 4) {
                    homing += 0.04f;
                }
                NPC targett = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1200);

                if (targett != null) {
                    if (Projectile.timeLeft < 60) {
                        Projectile.timeLeft = 60;
                    }
                    Projectile.velocity += (targett.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homing * 2;
                }
            }
            spt.Position = Projectile.Center;
            spt.Velocity = Projectile.velocity;
        }
        float homing = 0;

        public override void OnKill(int timeLeft) {
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, Color.LightBlue, 0.02f).Configure(new Vector2(2f, 2f), 0, 0.85f * 0.4f, 18);
            for (int i = 0; i < 5; i++) {
                PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(16, 36), Color.White, Main.rand.NextFloat(0.6f, 1.2f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
            CEUtils.PlaySound(Main.rand.NextBool() ? "scholarStaffImpact" : "scholarStaffImpact2", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
        }

        public int tofs;
        float alpha_ = 1;
        public override bool PreDraw(ref Color lightColor) {
            CEUtils.DrawGlow(Projectile.Center, Color.LightBlue * 0.5f, 1.8f);
            CEUtils.DrawGlow(Projectile.Center, Color.LightBlue, 0.2f);
            return false;
        }


    }
    public class StarblightRogue : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 12;
        }
        public int counter = 0;
        public bool std = false;
        public int homingTime = 60;
        public PRT_StarTrailParticle spt = null;
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[0] < 20) {
                return false;
            }
            return null;
        }
        public override void AI() {
            if (spt == null) {
                spt = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.Zero, Color.LightBlue, 1.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                spt.maxLength = 18;
            }
            spt.Velocity = Projectile.velocity;
            spt.Lifetime = 30;
            counter++;
            Projectile.ai[0]++;

            NPC target = Projectile.FindTargetWithinRange(1600, false);
            if (target != null && CEUtils.getDistance(target.Center, Projectile.Center) < 200 && counter > 20) {
                homingTime = 0;
                Projectile.velocity *= 0.9f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();

                Projectile.velocity += v * 1.5f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();


            if (Projectile.velocity.Length() > 3) {
                Projectile.velocity *= 0.995f - homing * 0.018f;
            }
            if (counter > 2) {
                if (homing < 4) {
                    homing += 0.015f;
                }
                NPC targett = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1200);

                if (targett != null) {
                    if (Projectile.timeLeft < 60) {
                        Projectile.timeLeft = 60;
                    }
                    Projectile.velocity += (targett.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homing * 2;
                }
            }
        }
        float homing = 0;

        public override void OnKill(int timeLeft) {
            //DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, Color.LightBlue, 0.02f).Configure(new Vector2(2f, 2f), 0, 0.85f * 0.4f, 18);
            for (int i = 0; i < 5; i++) {
                PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(16, 36), Color.White, Main.rand.NextFloat(0.6f, 1.2f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
            CEUtils.PlaySound(Main.rand.NextBool() ? "scholarStaffImpact" : "scholarStaffImpact2", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
        }

        public int tofs;
        float alpha_ = 1;
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }


    }
    public class FractalStarblight : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 260;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 12;
        }
        public int counter = 0;
        public bool std = false;
        public int homingTime = 60;
        public PRT_StarTrailParticle spt = null;
        public override void AI() {
            if (spt == null) {
                spt = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.Zero, Color.LightBlue, 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                spt.maxLength = 12;
            }
            spt.Velocity = Projectile.velocity;
            spt.Lifetime = 30;
            counter++;
            Projectile.ai[0]++;

            NPC target = Projectile.FindTargetWithinRange(1600, false);
            if (target != null && CEUtils.getDistance(target.Center, Projectile.Center) < 200 && counter > 30) {
                homingTime = 0;
                Projectile.velocity *= 0.9f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();

                Projectile.velocity += v * 1.5f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();


            if (Projectile.velocity.Length() > 3) {
                Projectile.velocity *= 0.995f - homing * 0.018f;
            }
            if (counter > 16) {
                if (homing < 4) {
                    homing += 0.1f;
                }
                NPC targett = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1200);

                if (targett != null) {
                    if (Projectile.timeLeft < 60) {
                        Projectile.timeLeft = 60;
                    }
                    Projectile.velocity += (targett.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homing * 2;
                }
            }
        }
        float homing = 0;

        public override void OnKill(int timeLeft) {
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, Color.LightBlue, 0.02f).Configure(new Vector2(2f, 2f), 0, 0.85f * 0.4f, 18);
            CEUtils.PlaySound("metalhit", Main.rand.NextFloat(1.6f, 2f), Projectile.Center, 6, 0.35f * CEUtils.WeapSound);
        }

        public int tofs;
        float alpha_ = 1;
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }


    }
}