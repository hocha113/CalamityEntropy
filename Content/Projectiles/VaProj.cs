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
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VaProj : ModProjectile
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
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 44;
            Projectile.height = 44;
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
            /*if (!std)
            {
                std = true;
                if (Main.myPlayer == Projectile.owner)
                {
                    for (int i = 4; i < 11; i++)
                    {
                        if (i % 2 == 0) 
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center - Projectile.velocity, Projectile.velocity * (float)i * 0.13f, ModContent.ProjectileType<VoidStarF>(), (int)(Projectile.damage * 0.6f), 6, Projectile.owner);
                        }
                    }
                }
            }*/
            counter++;
            if (Projectile.ai[0] == 0) {
                angle = Projectile.velocity.ToRotation();
            }
            if (speed < 0) {
                angle = (Projectile.Center - Main.player[Projectile.owner].Center).ToRotation();
                if (CEUtils.getDistance(Projectile.Center, Main.player[Projectile.owner].Center) < Projectile.velocity.Length() * 1.12f) {
                    Projectile.Kill();
                }
            }

            if (!htd) {
                Dust.NewDust(Projectile.Center, 16, 16, DustID.MagicMirror, Projectile.velocity.X * -0.1f, Projectile.velocity.Y * -0.1f);
                Dust.NewDust(Projectile.Center, 16, 16, DustID.MagicMirror, Projectile.velocity.X * -0.1f, Projectile.velocity.Y * -0.1f);
                if (counter % 14 == 0) {
                }
            }

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
                if (odp.Count > 64) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }

                NPC target = Projectile.FindTargetWithinRange(1800, false);
                if (target != null && CEUtils.getDistance(target.Center, Projectile.Center) < 200 && counter > 16) {
                    homingTime = 0;
                    Projectile.velocity *= 0.9f;
                    Vector2 v = target.Center - Projectile.Center;
                    v.Normalize();

                    Projectile.velocity += v * 1.5f;
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
                    Vector2 sparkVelocity2 = new Vector2(16, 0).RotatedByRandom(3.14159f) * Main.rand.NextFloat(0.5f, 1.8f);
                    int sparkLifetime2 = Main.rand.Next(20, 24);
                    float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                    Color sparkColor2 = Color.DarkBlue;

                    float velc = 0.6f;
                    if (Main.rand.NextBool()) {
                        //PRT_AltSpark跟LineCal随机混用,旧Calamity spark/Lines二选一
                        PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, sparkColor2, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                    }
                    else {
                        //LineCal Configure(false,lifetime)对齐Calamity LineParticle
                        PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, Main.rand.NextBool() ? Color.Purple : Color.Purple, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                    }
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
            Color result = new Color(30, 40, 75);
            return result * completionRatio;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 24 * Projectile.scale, completionRatio);
        }

        public override bool PreDraw(ref Color lightColor) {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            Color cl = Color.Lerp(Color.Black, Color.White, Projectile.ai[0] / 30f);
            float c = 0;


            c = 0;
            if (odp.Count > 1) {
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.Blue * 0.7f;
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 4,
                          new Vector3((float)0, 1, 1),
                          b));
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 4,
                      new Vector3((float)0, 0, 1),
                      b));
                for (int i = 1; i < odp.Count; i++) {


                    c += 1f / odp.Count;
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 20,
                          new Vector3((float)i / odp.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 20,
                          new Vector3((float)i / odp.Count, 0, 1),
                          b));

                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.rvslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                if (htd) { return false; }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(TextureAssets.Projectile[Projectile.type].Value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, TextureAssets.Projectile[Projectile.type].Value.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            }
            odp.RemoveAt(odp.Count - 1);
            odr.RemoveAt(odr.Count - 1);
            tofs++;
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            Vector2 position = base.Projectile.Center - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
            Vector2 origin = value.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.SylvestaffStreakAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(value, position, null, base.Projectile.GetAlpha(Color.White), base.Projectile.rotation, origin, base.Projectile.scale, SpriteEffects.None);
            return true;
        }

    }

}