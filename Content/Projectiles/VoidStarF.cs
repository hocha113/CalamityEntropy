using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VoidStarF : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public float Hue => 0.55f;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 400;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.extraUpdates = 1;
        }
        public override bool? CanHitNPC(NPC target) {
            if (counter < 16) {
                return false;
            }
            return null;
        }
        public bool setv = true;
        public int counter = 0;
        public override void AI() {
            if (Projectile.ai[2] > 0) {
                Projectile.DamageType = DamageClass.Magic;
                //HeavenfallStar拖尾,旧PRT/EParticle HeavenfallStar数值照抄
                var __prt = PRTLoader.NewParticle<PRT_HeavenfallStar>(Projectile.Center, Projectile.velocity.normalize(), new Color(255, 120, 120), Main.rand.NextFloat(0.6f, 1.3f) * 1.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 14);
                __prt.xScale = 0.14f;
                var __prt2 = PRTLoader.NewParticle<PRT_HeavenfallStar>(Projectile.Center - Projectile.velocity / 2f, Projectile.velocity.normalize(), new Color(255, 120, 120), Main.rand.NextFloat(0.6f, 1.3f) * 1.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 14);
                __prt2.xScale = 0.14f;
            }
            else {
                Projectile p = Projectile;
                if (Main.rand.NextBool(2)) {
                    //形体烟Cal+后面EHeavySmoke发光层的话后者Additive走Configure
                    PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.velocity * 0.5f, Color.Lerp(Color.DodgerBlue.MultiplyRGB(p.ai[2] > 0 ? new Color(255, 80, 80) : Color.White), Color.MediumVioletRed.MultiplyRGB(Projectile.DamageType == DamageClass.Magic ? new Color(255, 140, 140) : Color.White), (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f)), Main.rand.NextFloat(0.6f, 1.2f) * Projectile.scale).Configure(0.28f, 20, 0, false, 0, true);

                    if (Main.rand.NextBool(3)) {
                        PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.velocity * 0.5f, Main.hslToRgb(Hue, 1, 0.7f).MultiplyRGB(p.ai[2] > 0 ? new Color(255, 80, 80) : Color.White), Main.rand.NextFloat(0.4f, 0.7f) * Projectile.scale).Configure(0.8f, 15, 0, true, 0.05f, true);
                    }
                }
            }
            counter++;
            if (setv) {
                setv = false;
                Projectile.velocity *= 0.5f;
            }
            odp.Add(Projectile.Center);
            if (odp.Count > 24) {
                odp.RemoveAt(0);
            }
            Projectile.velocity *= 0.999f;
            if (Projectile.timeLeft < 360) {
                NPC target = Projectile.FindTargetWithinRange(4000, false);
                if (target != null) {
                    Projectile.velocity *= 0.99f;
                    Vector2 v = target.Center - Projectile.Center;
                    v.Normalize();

                    Projectile.velocity += v * 0.4f;
                    if (CEUtils.getDistance(Projectile.Center, target.Center) < 180) {
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((target.Center - Projectile.Center).ToRotation());
                    }
                }
            }
            if (Projectile.timeLeft < 40) {
                Projectile.alpha += 255 / 40;
            }
            Projectile.rotation += 0.1f;
            Lighting.AddLight(Projectile.Center, 0.75f, 1f, 0.24f);


        }

        public override bool PreDraw(ref Color lightColor) {

            return false;
        }
    }

}