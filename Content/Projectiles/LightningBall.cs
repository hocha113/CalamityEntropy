using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LightningBall : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 1024;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 9;
        }
        public PRT_TrailParticle t1;
        public PRT_TrailParticle t2;
        public List<PRT_TrailParticle> ts = new List<PRT_TrailParticle>();
        public override void AI() {
            if (Projectile.localAI[1] == 0) {
                var trailColor = Projectile.ai[1] == 1 ? Color.Red : Color.White;
                for (int i = 0; i < 4; i++) {
                    //TrailParticle不开CanPool,odp轨迹List池化会闪上一条
                    var pt = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, trailColor, Projectile.scale * 0.56f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
                    pt.maxLength = 11;
                    ts.Add(pt);
                }
                t1 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, trailColor, Projectile.scale * 0.5f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
                t2 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, trailColor, Projectile.scale * 0.5f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
                t1.maxLength *= 4;
                t2.maxLength *= 4;
            }
            ++Projectile.localAI[1];
            t1.Lifetime = 13;
            t2.Lifetime = 13;
            foreach (var p in ts) {
                p.Lifetime = 13;
                if (Main.rand.NextBool(8)) {
                    p.AddPoint(Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-26, 26) * Projectile.scale);
                }
            }
            t1.AddPoint(Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).normalize() * (float)(Math.Cos(Projectile.localAI[1] * 0.1f)) * (7 * Projectile.scale));
            t2.AddPoint(Projectile.Center + Projectile.velocity.RotatedBy(-MathHelper.PiOver2).normalize() * (float)(Math.Cos(Projectile.localAI[1] * 0.1f)) * (7 * Projectile.scale));
        }

        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("ofhit", 1, Projectile.Center);
            for (int i = 0; i < 16; i++) {
                //TrailSparkParticle跟TrailParticle成对spawn,旧trail+spark一套
                PRTLoader.NewParticle<PRT_TrailSparkParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 14), (Projectile.ai[1] == 1 ? Color.Red : Color.White), Projectile.scale).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, (Projectile.ai[1] == 1 ? Color.Red : Color.White), 0.1f).Configure(new Vector2(2f, 2f), 0, (false ? 2 : 1) * 0.85f * 0.4f, (false ? 46 : 36));
            PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, (Projectile.ai[1] == 1 ? Color.Red : Color.White), 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), (false ? 2.2f : 1) * 0.65f * 0.4f, (false ? 30 : 26));
            if (Main.myPlayer == Projectile.owner) {
                Projectile projectile = Projectile;
                for (int i = 0; i < 3 + Projectile.owner.ToPlayer().Entropy().WeaponBoost; i++) {
                    Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, new Vector2(30, 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), ModContent.ProjectileType<Lightning>(), (int)(projectile.damage * 0.45f), 4, projectile.owner, 0, 0, Projectile.ai[1]).ToProj().DamageType = DamageClass.Magic;
                }
            }
            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/ExoTwinsEject"), Projectile.Center);
            //原灾厄震屏只在拥有者本端可见, 换自有屏震后补拥有者判定保持一致(原强度7)
            if (Main.myPlayer == Projectile.owner && CEUtils.getDistance(Projectile.Center, Projectile.GetOwner().Center) < 1200) {
                CalamityEntropy.Instance.screenShakeAmp = 2.5f;
            }
        }


        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D light = CEExtraAssets.a_circle;
            Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition, null, (Projectile.ai[1] == 1 ? Color.Red : new Color(128, 128, 128)), 0, light.Size() / 2, 0.6f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition, null, (Projectile.ai[1] == 1 ? new Color(255, 200, 200) : Color.White), 0, light.Size() / 2, 0.4f * Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }


}