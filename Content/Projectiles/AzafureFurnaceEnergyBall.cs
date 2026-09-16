using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AzafureFurnaceEnergyBall : ModProjectile
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
            if (Main.zenithWorld) {
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = 12;
                Projectile.penetrate = 5;
                Projectile.tileCollide = false;
            }
            Projectile.extraUpdates = 9;
        }
        public PRT_TrailParticle t1;
        public PRT_TrailParticle t2;
        public PRT_TrailParticle t3;
        public PRT_TrailParticle t4;
        public List<PRT_TrailParticle> ts = new List<PRT_TrailParticle>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<MechanicalTrauma>(340);
            if (Main.zenithWorld) {
                OnKill(Projectile.timeLeft);
            }
        }
        public override void AI() {
            if (Projectile.localAI[1] == 0) {
                var trailColor = Projectile.ai[1] == 1 ? Color.Red : Color.White;
                //TrailParticle不开CanPool,odp轨迹List池化会闪上一条
                t1 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, trailColor, Projectile.scale * 0.5f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
                t2 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, trailColor, Projectile.scale * 0.5f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
                t3 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, trailColor, Projectile.scale * 0.5f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
                t4 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, trailColor, Projectile.scale * 0.5f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0);
                t1.maxLength *= 4;
                t2.maxLength *= 4;
                t3.maxLength *= 4;
                t4.maxLength *= 4;
            }
            ++Projectile.localAI[1];
            t1.Lifetime = 13;
            t2.Lifetime = 13;
            t3.Lifetime = 13;
            t4.Lifetime = 13;
            t1.AddPoint(Projectile.Center + Projectile.velocity.RotatedBy(Projectile.localAI[1] * 0.1f).normalize() * (16 * Projectile.scale));
            t2.AddPoint(Projectile.Center + Projectile.velocity.RotatedBy(Projectile.localAI[1] * 0.1f + MathHelper.PiOver2).normalize() * (16 * Projectile.scale));
            t3.AddPoint(Projectile.Center + Projectile.velocity.RotatedBy(Projectile.localAI[1] * 0.1f + MathHelper.Pi).normalize() * (16 * Projectile.scale));
            t4.AddPoint(Projectile.Center + Projectile.velocity.RotatedBy(Projectile.localAI[1] * 0.1f - MathHelper.PiOver2).normalize() * (16 * Projectile.scale));
            Main.dust[Dust.NewDust(Projectile.Center + CEUtils.randomPointInCircle(12), 0, 0, DustID.GemRuby, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f)].noGravity = true;
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 800);
            if (target != null && Projectile.localAI[1] > 60) {
                Projectile.velocity += (target.Center - Projectile.Center).normalize() * 0.08f;
                Projectile.velocity *= 0.99f;
            }
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("ofhit", 1, Projectile.Center);
            for (int i = 0; i < 16; i++) {
                //TrailSparkParticle跟TrailParticle成对spawn,旧trail+spark一套
                PRTLoader.NewParticle<PRT_TrailSparkParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 16), (Projectile.ai[1] == 1 ? Color.Red : Color.White), Projectile.scale * 1.4f).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0, 36);
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, (Projectile.ai[1] == 1 ? Color.Red : Color.White), 0.1f).Configure(new Vector2(2f, 2f), 0, 0.3f, 32);
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, (Projectile.ai[1] == 1 ? Color.Red : Color.White), 0.1f).Configure(new Vector2(2f, 2f), 0, 0.4f, 36);
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, (Projectile.ai[1] == 1 ? Color.Red : Color.White), 0.1f).Configure(new Vector2(2f, 2f), 0, 0.5f, 40);
            if (Main.myPlayer == Projectile.owner) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FurnaceBlast>(), 0, 0, Projectile.owner, Projectile.ai[1]);
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.GetOwner(), Projectile.Center, Projectile.damage * 2, 240, Projectile.DamageType);
            }
            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/ExoTwinsEject"), Projectile.Center);
            if (CEUtils.getDistance(Projectile.Center, Projectile.GetOwner().Center) < 1200) {
                CEUtils.SetShake(Projectile.Center, 7, 1200);
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