using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class WelkinFeather : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void SetDefaults() {
            // 原盗贼职业并入原版：天穹武器裁定为近战（rogue-weapons §二 AzureOfFirmament）
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 0.16f;
            Projectile.timeLeft = 340;
            Projectile.extraUpdates = 1;
        }
        public bool setv = true;
        bool c = true;
        public float homingStrength = 0;
        public bool flag = true;
        public NPC homingTarget { get { if (Projectile.ai[1] < 0) { return null; } else { return ((int)(Projectile.ai[1])).ToNPC(); } } }
        public override void AI() {
            if (c) {
                c = false;
                Projectile.ai[1] = -1;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            if (homingTarget != null && homingTarget.active) {
                for (float i = 0; i <= 1; i += 0.5f) {
                    Vector2 velocity1 = CEUtils.randomPointInCircle(1.6f);
                    //PRT_CritSparkCal Calamity crit spark,Configure Ports签名
                    PRTLoader.NewParticle<PRT_CritSparkCal>(Projectile.Center - Projectile.velocity * i + Projectile.velocity * 0.6f, velocity1, Color.White * 0.6f, 0.26f).Configure(Color.SkyBlue, 12, 0.1f, 3f, Main.rand.NextFloat(0f, 0.01f));
                }
                if (flag) {
                    flag = false;
                    for (int i = 0; i < 8; i++) {
                        Vector2 velocity = ((MathHelper.TwoPi * i / 8) - (MathHelper.Pi / 16f)).ToRotationVector2() * 6f;
                        //CritSparkCal Calamity crit spark,Configure Ports签名
                        PRTLoader.NewParticle<PRT_CritSparkCal>(Projectile.Center, velocity, Color.White, 0.8f).Configure(Color.SkyBlue, 30, 0.1f, 3f, Main.rand.NextFloat(0f, 0.01f));
                    }
                    Projectile.velocity = new Vector2(Projectile.velocity.Length() * 3, 0).RotatedBy((homingTarget.Center - Projectile.Center).ToRotation());
                }
                NPC target = homingTarget;
                if (homingStrength < 1) { homingStrength += 0.01f; }
                Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 2f * homingStrength;
                Projectile.velocity *= 1 - homingStrength * 0.1f;

            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("GrassSwordHit1", Main.rand.NextFloat(1.6f, 1.9f), target.Center, 20, 0.3f);
            float r = Main.GameUpdateCount * 0.064f;
            for (int i = 0; i < 6; i++) {
                Vector2 velocity = ((MathHelper.TwoPi * i / 6) - (MathHelper.Pi / 16f) + r).ToRotationVector2() * 10f;
                PRTLoader.NewParticle<PRT_CritSparkCal>(target.Center, velocity, Color.White, 0.6f).Configure(Color.SkyBlue, 20, 0.1f, 3f, Main.rand.NextFloat(0f, 0.01f));
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D t = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, t.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            if (homingTarget != null && homingTarget.active) {
                t = this.getTextureAlt("Outline");
                Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.AliceBlue, Projectile.rotation, t.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }

}