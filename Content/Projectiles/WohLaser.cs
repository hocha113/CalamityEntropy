using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class WohLaser : ModProjectile
    {
        public int length = 2400;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.ai[1] = 1;
            Projectile.ArmorPenetration = 36;
        }
        public override void AI() {
            if (Projectile.ai[2] == 0)
                Projectile.Center = Projectile.GetOwner().MountedCenter;
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (float i = 0; i < 2000; i += 100) {
                //HeavySmokeCal Configure是Calamity原构造顺序,跟PRT/EParticle统一尾参不是一回事
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(
                    Projectile.Center - Projectile.GetOwner().velocity + Projectile.velocity.normalize() * (i + Main.rand.NextFloat(0, 200)),
                    Projectile.velocity.normalize() * 26 * Main.rand.NextFloat() + CEUtils.randomPointInCircle(1) + Projectile.GetOwner().velocity,
                    Color.Lerp(Color.DeepSkyBlue, Color.DarkBlue, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.8f, 2f) * Projectile.ai[1]).Configure(0.3f, 12, Main.rand.NextFloat(-0.1f, 0.1f), false);
            }

            if (Projectile.ai[0] > 4) {
                Projectile.ai[1] -= 1 / 8f;
            }
            else {
                Projectile.ai[1] += 0.25f;
            }
            Projectile.ai[0]++;
            if (Projectile.owner == Main.myPlayer) {
                Projectile.netUpdate = true;
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.ai[0] > 4) {
                return false;
            }
            float laserLength = Projectile.scale * length;
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.One) * laserLength, targetHitbox, 90);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("beast_lavaball_rise1", Main.rand.NextFloat(1.4f, 1.8f), target.Center);
            float sparkCount = 16;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = Projectile.velocity.RotateRandom(0.2f) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime2 = Main.rand.Next(20, 24);
                float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                Color sparkColor2 = Color.DarkBlue;

                float velc = 1f;
                //跟AltSpark成对出现时寿命/速度系数是旧代码原值
                PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, Main.rand.NextBool() ? Color.Purple : Color.Purple, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));

            }
            for (int i = 0; i < 29; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.rotation.ToRotationVector2() * CEUtils.getDistance(target.Center, Projectile.Center), ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(3f, 3.5f);
                dust.velocity =
                    Projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(8, 36);
                dust.noGravity = true;
                dust.color = Color.LightBlue;
                dust.fadeIn = 2f;
            }
            if (Projectile.ai[2] > 0) {
                EGlobalNPC.AddVoidTouch(target, 50, 5, 600, (int)Projectile.ai[2]);
            }
            else {
                EGlobalNPC.AddVoidTouch(target, 50, 5);
            }
            Projectile.damage = (int)(Projectile.damage * 0.8f);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
            Texture2D tex = CEExtraAssets.Streak1;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 900), 0, (int)(Projectile.scale * length), tex.Height), new Color(80, 60, 255), Projectile.rotation, new Vector2(0, tex.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.37f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 1400), 0, (int)(Projectile.scale * length), tex.Height), new Color(160, 140, 255), Projectile.rotation, new Vector2(0, tex.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.22f), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

    }

}