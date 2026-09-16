using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NihilityEnergyBall : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 1600;
        }
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float rotn = 0;
        float al = 0;
        public float rotj = 0.4f;
        public float dist = 0;
        public float adp = 0;
        public override void AI() {
            if (Projectile.localAI[0] > 80) {
                adp += 3f;
            }
            dist += ((Projectile.localAI[0] > 80 ? 200 : 100) - dist) * 0.04f;
            NPC n = ((int)Projectile.ai[0]).ToNPC();
            rotn += rotj;
            rotj *= 0.98f;
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < 140) {
                Projectile.Center = n.Center + (Projectile.ai[1] + rotn).ToRotationVector2() * (dist + adp);
            }
            else {
                if (Projectile.localAI[0] == 140) {
                    Projectile.velocity = (Projectile.Center - n.Center).SafeNormalize(Vector2.Zero) * 40;
                }
            }
            Projectile.velocity *= 0.98f;
            if (Projectile.localAI[0] > 200) {
                Projectile.velocity *= 0.98f;
                if (al < 1) {
                    al += 0.025f;
                }
                Projectile.velocity += (n.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 1.8f;
                if (CEUtils.getDistance(Projectile.Center, n.Center) < Projectile.velocity.Length() * 1.2f + 40) {
                    if (Projectile.timeLeft > 30) {
                        Projectile.timeLeft = 30;
                    }
                }
            }
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 28) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
            if (Projectile.timeLeft < 33) {
                if (odp.Count > 0) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
                if (odp.Count > 0) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(200, 200, 255);
            return result * completionRatio;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 66 * Projectile.scale, completionRatio);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);
        }
        public override bool PreDraw(ref Color lightColor) {
            drawT();
            return true;
        }

        public void drawT() {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            var mp = this;
            if (mp.odp.Count > 1) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(100, 100, 155);

                float a = 0;
                float lr = 0;
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 22,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 22,
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
                }


            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            Vector2 position = base.Projectile.Center - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
            Vector2 origin = value.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.SylvestaffStreakAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < 8; i++) {
                for (int j = 0; j <= 3; j++) {
                    Main.EntitySpriteDraw(value, position + MathHelper.ToRadians(i * (360f / 8f)).ToRotationVector2() * 2, null, Color.White * al, base.Projectile.rotation, origin, base.Projectile.scale, SpriteEffects.None);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }

    }

}