using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class WohShot : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float angle;
        public float speed = 30;
        public bool htd = false;
        public float exps = 0;
        public Vector2 dscp = Vector2.Zero;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.extraUpdates = 2;
            Projectile.ArmorPenetration = 12;
        }
        public int counter = 0;
        public bool std = false;
        public int homingTime = 60;
        public override void AI() {
            counter++;

            Projectile.ai[0]++;
            if (htd) {
                if (odp.Count > 0) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);

                }
                if (odp.Count > 0) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
                Projectile.velocity = Vector2.Zero;
            }
            else {
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation);
                if (odp.Count > 18) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
            }
            exps *= 0.9f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (htd) {
                return false;
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (!htd) {
                float sparkCount = 12;
                for (int i = 0; i < sparkCount; i++) {
                    Vector2 sparkVelocity2 = new Vector2(32, 0).RotatedBy(Projectile.velocity.ToRotation()).RotatedByRandom(0.2f) * Main.rand.NextFloat(0.5f, 1.8f);
                    int sparkLifetime2 = Main.rand.Next(20, 24);
                    float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                    Color sparkColor2 = Color.Purple;

                    float velc = 0.6f;

                    //PRT_LineCal Configure(false,lifetime)对齐Calamity LineParticle
                    PRTLoader.NewParticle<PRT_LineCal>(Projectile.Center + Projectile.velocity * 1.2f, sparkVelocity2 * velc, Main.rand.NextBool() ? Color.AliceBlue : Color.Purple, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));  //跟AltSpark成对出现时寿命/速度系数是旧代码原值

                }
                EGlobalNPC.AddVoidTouch(target, 30, 1);
                Projectile.timeLeft = 20;
                htd = true;
                exps = 1;
                odp.Add(Projectile.Center);
                odr.Add(Projectile.rotation);
            }
        }
        public int tofs;
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(80, 60, 255);
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            if (completionRatio > 0.92f) {
                return 22 * Projectile.scale * 1.4f * MathHelper.SmoothStep(0, 1, (1 - (completionRatio - 0.92f) / 0.08f));
            }
            return MathHelper.Lerp(0, 26 * Projectile.scale * 1.4f, completionRatio);
        }
        public Color TrailColor2(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255);
            return result;
        }

        public float TrailWidth2(float completionRatio, Vector2 vertex) {
            if (completionRatio > 0.92f) {
                return 14 * Projectile.scale * 1.4f * MathHelper.SmoothStep(0, 1, (1 - (completionRatio - 0.92f) / 0.08f));
            }
            return MathHelper.Lerp(0, 14 * Projectile.scale * 1.4f, completionRatio);
        }

        public override bool PreDraw(ref Color lightColor) {
            drawT();
            return false;
        }

        public void drawT() {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            tofs++;
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            Vector2 position = base.Projectile.Center - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
            Vector2 origin = value.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.Streak1Asset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth2, TrailColor2, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(value, position, null, base.Projectile.GetAlpha(Color.White), base.Projectile.rotation, origin, base.Projectile.scale, SpriteEffects.None);

        }

    }

}