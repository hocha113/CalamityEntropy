using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class MercyShoot : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.ArmorPenetration = 256;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 360);
            int boost = Projectile.owner.ToPlayer().Entropy().WeaponBoost;
            if (boost > 0) {
                EGlobalNPC.AddVoidTouch(target, 5, boost * 0.2f, 800, 10 + 6 * boost);
            }
        }
        Color? colorset = null;
        public override void AI() {
            Projectile projectile = Projectile;
            if (colorset == null) {
                colorset = Color.Lerp(Color.Red, new Color(170, 50, 50), (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f));
                if (Projectile.owner.ToPlayer().Entropy().WeaponBoost > 0) {
                    colorset = Color.Lerp(Color.Purple, Color.Indigo, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f));
                }
            }
            Color color = (Color)colorset;
            Vector2 direction = new Vector2(-1, 0).RotatedBy(projectile.velocity.ToRotation());
            Vector2 smokeSpeed = direction.RotatedByRandom(MathHelper.PiOver4 * 0.1f) * Main.rand.NextFloat(10f, 30f) * 0.9f;
            //形体HeavySmokeCal+发光EHeavySmoke两层,后者AdditiveBlend走Configure(类头有写)
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(projectile.Center + direction * 46f, smokeSpeed + projectile.velocity, color, Main.rand.NextFloat(0.6f, 1.2f)).Configure(0.8f, 20, 0, false, 0, true);

            if (Main.rand.NextBool(2)) {
                //EHeavySmoke AdditiveBlend+Spin/HueShift字段,Configure只管opacity和分桶
                var smokeGlow = PRTLoader.NewParticle<PRT_EHeavySmoke>(
                    projectile.Center + direction * 46f,
                    smokeSpeed + projectile.velocity,
                    Projectile.owner.ToPlayer().Entropy().WeaponBoost > 0 ? Main.hslToRgb(0.85f, 1, 0.8f) : new Color(255, 85, 0),
                    Main.rand.NextFloat(0.4f, 0.7f))
                    .Configure(0.8f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 16);
                smokeGlow.Spin = 0.01f;
                smokeGlow.HueShift = 0.01f;
                smokeGlow.StrongVisual = true;
            }

        }


        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }


}