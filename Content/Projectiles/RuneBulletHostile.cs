using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RuneBulletHostile : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float angle;
        public float speed = 30;
        public bool htd = false;
        public float exps = 0;
        public Vector2 dscp = Vector2.Zero;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1;
            Projectile.timeLeft = 4 * 70;
        }
        public int counter = 0;
        public bool std = false;
        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            hitbox = Projectile.Center.getRectCentered(38 * Projectile.scale, 38 * Projectile.scale);
        }
        public override void AI() {
            base.AI();
            counter++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.Length() < Projectile.ai[0]) {
                Projectile.velocity *= 1.02f;
            }
            float j = (float)Math.Cos(Main.GlobalTimeWrappedHourly * 2) * 12 * Projectile.scale;
            Dust.NewDust(Projectile.Center + Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * j, 1, 1, DustID.MagicMirror);
        }
        public override void PostAI() {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 8) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public int tofs;
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255) * completionRatio;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 12 * Projectile.scale, completionRatio);
        }

        public override bool PreDraw(ref Color lightColor) {
            drawT();
            return false;
        }
        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
            for (int i = 0; i < 10; i++) {
                //GlowSpark旧PRT/EParticle,Configure尾参统一签名那套
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7), Color.LightBlue, Main.rand.NextFloat(0.06f, 0.1f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            for (int i = 0; i < 10; i++) {
                //GlowSpark旧EParticle,Configure尾参统一签名那套
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 7), Color.LightBlue, Main.rand.NextFloat(0.06f, 0.1f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
        }
        public Color color = new Color(188, 149, 255);
        public void drawT() {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            var mp = this;
            if (mp.odp.Count > 1) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = this.color * 0.66f;
                b.A = 255;
                float a = 0;
                float lr = 0;
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                          b * a));
                    lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.wohslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                    ve = new List<ColoredVertex>();
                    b = this.color;

                    a = 0;
                    lr = 0;
                    for (int i = 1; i < mp.odp.Count; i++) {
                        a += 1f / (float)mp.odp.Count;

                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                              b * a));
                        lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                    }
                    tx = CEExtraAssets.wohslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }


            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            tofs++;
            if (runetex == 0)
                runetex = Main.rand.Next(1, 12);

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.Streak1Asset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            Texture2D texture = CEUtils.getExtraTex("runes/rune" + runetex.ToString());
            if (odp.Count > 1) {
                Vector2 position = odp[odp.Count - 1] - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
                Vector2 origin = texture.Size() * 0.5f;
                CEUtils.DrawGlow(position + Main.screenPosition, Color.Lerp(Color.White, this.color * 1.1f, (float)(Math.Cos(Main.GlobalTimeWrappedHourly) * 0.5 + 0.5)), Projectile.scale * 0.6f);

                Main.EntitySpriteDraw(texture, position, null, base.Projectile.GetAlpha(Color.White), 0, origin, base.Projectile.scale * 0.5f, SpriteEffects.None);

            }

        }
        public int runetex = 0;
    }

}