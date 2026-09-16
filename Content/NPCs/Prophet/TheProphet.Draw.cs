using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Prophet.Core;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    /// <summary>
    /// 先知的绘制层:纯本地,只读状态与表现量,不回写任何 gameplay 状态。
    /// 本体、鳍、尾迹都读同一个平滑层级 —— <c>NPC.Center</c> 的原始值
    /// (<c>NoMultiplayerSmoothingByType</c> 已关掉原版偏移,纠偏器每帧把 <c>netOffset</c> 清零),
    /// 所以尾迹接缝不会在收包时跳开
    /// </summary>
    public partial class TheProphet
    {
        //绘制用贴图,加载期由 VaultLoaden 赋值,只在客户端绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/NPCs/Prophet/Wing", 1, 2, AssetMode = AssetMode.TextureValueArray)]
        private static Texture2D[] fintexs;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Prophet/Tail")]
        private static Asset<Texture2D> tailTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Prophet/ring")]
        private static Asset<Texture2D> ringTex;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (Main.zenithWorld) {
                DrawTail();
                DrawFins();
                return zenithAI.PreDraw(NPC, spriteBatch, screenPos, drawColor);
            }
            else {
                Draw();
            }
            return false;
        }

        public void Draw() {
            //天顶世界走上面那条分支,这一行到不了;原代码就有,照搬
            if (Main.zenithWorld) {
                spawnAnm = 0;
            }
            if (spawnAnm < ProphetDirector.SpawnAnimDrawFrames) {
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
                DrawTail();
                DrawFins();
                Texture2D tex = TextureAssets.Npc[NPC.type].Value;
                Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, Color.White, rl + MathHelper.PiOver2, tex.Size() / 2, NPC.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            }
            if (spawnAnm > 0) {
                float a = (float)Math.Sin(spawnAnm / 120f * MathHelper.Pi);
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = GP(0);
                    List<Vector2> pointsOutside = GP(360 * a);
                    int i;
                    for (i = 0; i < points.Count; i++) {
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + points[i],
                        new Vector3((float)i / points.Count, 1, 0.9f),
                              Color.SkyBlue * a));
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 0.9f),
                              Color.SkyBlue * a));

                    }
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3) {
                        Texture2D tx = CEExtraAssets.AbyssalCircle2;
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = GP(0, -1);
                    List<Vector2> pointsOutside = GP(420 * a, -1);
                    int i;
                    for (i = 0; i < points.Count; i++) {
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + points[i],
                              new Vector3((float)i / points.Count, 1, 0.9f),
                              Color.White * a));
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 0.9f),
                              Color.White * a));

                    }
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3) {
                        Texture2D tx = CEExtraAssets.AbyssalCircle2;
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = GP(0, 0.6f);
                    List<Vector2> pointsOutside = GP(420 * a, -1);
                    int i;
                    for (i = 0; i < points.Count; i++) {
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + points[i],
                              new Vector3((float)i / points.Count, 1, 0.9f),
                              Color.SkyBlue * a));
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 0.9f),
                              Color.SkyBlue * a));

                    }
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3) {
                        Texture2D tx = CEExtraAssets.AbyssalCircle2;
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
        }

        /// <summary>出生光环的取点:61 个点绕一圈,随全局时间反向旋转,c 控制转速与方向</summary>
        public List<Vector2> GP(float distAdd = 0, float c = 1) {
            float dist = distAdd;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 60; i++) {
                points.Add(new Vector2(dist, 0).RotatedBy(MathHelper.ToRadians(i * 6 - 80 * c * Main.GlobalTimeWrappedHourly)));
            }
            return points;
        }

        public void DrawFins() {
            float rotj = finRotCounter <= 0.4f ? CEUtils.GetRepeatedCosFromZeroToOne(finRotCounter / 0.4f, 1) : 1 - CEUtils.GetRepeatedCosFromZeroToOne((finRotCounter - 0.4f) / 0.6f, 1);
            Main.EntitySpriteDraw(fintexs[1], NPC.Center + new Vector2(-20, -20).RotatedBy(NPC.rotation) - Main.screenPosition, null, Color.White, rl - rotj, fintexs[1].Size(), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(fintexs[1], NPC.Center + new Vector2(-20, 20).RotatedBy(rl) - Main.screenPosition, null, Color.White, rl + rotj, fintexs[1].Size() * new Vector2(1, 0), NPC.scale, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(fintexs[0], NPC.Center + new Vector2(0, -20).RotatedBy(rl) - Main.screenPosition, null, Color.White, rl - 1 + rotj, new Vector2(fintexs[0].Width * 0.5f, fintexs[0].Height), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(fintexs[0], NPC.Center + new Vector2(0, 20).RotatedBy(rl) - Main.screenPosition, null, Color.White, rl + 1 - rotj, new Vector2(fintexs[0].Width * 0.5f, 0), NPC.scale, SpriteEffects.FlipVertically);
        }

        public void DrawTail() {
            if (tail == null || tail.Count < 3) {
                return;
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = Color.White;

            for (int i = 0; i < tail.Count - 3; i++) {
                ve.Add(new ColoredVertex(tail[i].position - Main.screenPosition + (tail[i + 1].position - tail[i].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 40,
                      new Vector3((((float)i) / tail.Count), 1, 1),
                      b));
                ve.Add(new ColoredVertex(tail[i].position - Main.screenPosition + (tail[i + 1].position - tail[i].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 40,
                      new Vector3((((float)i) / tail.Count), 0, 1),
                      b));

            }
            ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + (NPC.Center - tail[tail.Count - 1].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 40,
                      new Vector3((float)1, 1, 1),
                      b));
            ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + (NPC.Center - tail[tail.Count - 1].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 40,
                  new Vector3((float)1, 0, 1),
                  b));
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3) {
                Texture2D tx = tailTex.Value;
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Texture2D ring = ringTex.Value;
                if (tail.Count > 10) {
                    Main.EntitySpriteDraw(ring, tail[8].position - Main.screenPosition, null, Color.White, (tail[9].position - tail[8].position).ToRotation() - MathHelper.PiOver2, ring.Size() / 2f, NPC.scale, SpriteEffects.None);
                }
            }
        }
    }
}
