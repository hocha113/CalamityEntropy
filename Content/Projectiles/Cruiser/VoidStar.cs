using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class VoidStar : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public float Hue => 0.55f;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
        }
        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 480;
            Projectile.extraUpdates = 1;
        }
        public bool setv = true;

        public override void AI() {
            if (Projectile.ai[0] == 0)
                //CruiserWarn cruiser专属警告圈,旧PRT/EParticle CruiserWarn
                PRTLoader.NewParticle<PRT_CruiserWarn>(Projectile.Center, Projectile.velocity * 6, Color.White * 0.6f, 0.016f * Projectile.velocity.Length()).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation());
            Projectile.ai[0]++;
            if (Projectile.ai[2] == 1 && Projectile.ai[0] < 60) {
                return;
            }
            if (setv) {
                setv = false;
                Projectile.velocity *= 0.5f;
            }
            odp.Add(Projectile.Center);
            if (odp.Count > 24) {
                odp.RemoveAt(0);
            }
            Projectile.velocity *= 0.999f;

            if (Projectile.timeLeft < 40) {
                Projectile.alpha += 255 / 40;
            }

            Projectile.rotation += 0.1f;
            Lighting.AddLight(Projectile.Center, 0.75f, 1f, 0.24f);

            if (Main.rand.NextBool(5)) {
                //形体烟Cal+后面EHeavySmoke发光层的话后者Additive走Configure
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.velocity * 0.5f, Color.Lerp(Color.DodgerBlue, Color.MediumVioletRed, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f)), Main.rand.NextFloat(0.6f, 1.2f) * Projectile.scale).Configure(0.28f, 20, 0, false, 0, true);

                if (Main.rand.NextBool(3)) {
                    PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.velocity * 0.5f, Main.hslToRgb(Hue, 1, 0.7f), Main.rand.NextFloat(0.4f, 0.7f) * Projectile.scale).Configure(0.8f, 15, 0, true, 0.05f, true);
                }
            }
        }
        public override bool ShouldUpdatePosition() {
            if (Projectile.ai[2] == 1 && Projectile.ai[0] < 60) {
                return false;
            }
            return base.ShouldUpdatePosition();
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.ai[2] == 1) {
                if (Projectile.ai[0] < 60) {
                    CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, Projectile.Center, Projectile.Center + Projectile.velocity * 1000, Color.Purple * (0.8f * Projectile.ai[0] / 60f), 2);
                }
            }
            return false;
        }
    }

}