using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Prophet.Core;
using InnoVault.Rigs2D.Runtime;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    /// <summary>
    /// 先知的绘制层:纯本地,只读状态与表现量,不回写任何 gameplay 状态。
    /// 本体、翅、尾是同一副 Rigs2D 骨架(TheProphet.Rig.cs),读的是同一层位置:AI 开头 <c>CEBossNetMotion.BeginFrame</c> 已把
    /// <c>netOffset</c> 清零(<c>NoMultiplayerSmoothingByType</c> 也关掉了原版偏移),骨架根又是在 AI 里按 <c>NPC.Center</c> 推进的,
    /// 所以收包时尾根不会跳开
    /// </summary>
    public partial class TheProphet
    {
        //本体、两种翅、尾带与尾环的贴图都由 Rigs2D 骨架件持有(Assets/Rigs/Prophet.rig.json),这里不再声明贴图字段

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!RigReady) {
                return false;
            }
            if (Main.zenithWorld) {
                //天顶世界:骨架只出翅与尾(body 件已隐藏),本体仍由旧巡洋舰替身按原样绘制
                DrawRig(spriteBatch);
                return zenithAI.PreDraw(NPC, spriteBatch, screenPos, drawColor);
            }
            Draw();
            return false;
        }

        public void Draw() {
            //天顶世界走上面那条分支,这一行到不了;原代码就有,照搬
            if (Main.zenithWorld) {
                spawnAnm = 0;
            }
            if (spawnAnm < ProphetDirector.SpawnAnimDrawFrames) {
                DrawRig(Main.spriteBatch);
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

        /// <summary>
        /// 整副骨架按层序一遍画完:尾带(0)→ 尾环(1)→ 内翅两片(2、3)→ 外翅两片(4、5)→ 本体(6),与原绘制顺序一致。
        /// 件与带全是满亮(件 <c>unlit</c> + <c>Flat(Color.White)</c>,原来就是 <c>Color.White</c> 绘制);
        /// 带状件自己切一轮批次(Immediate)并回到 Deferred / AlphaBlend,调用方处在任意已 Begin 的批次内即可
        /// </summary>
        private void DrawRig(SpriteBatch spriteBatch) {
            Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(Color.White);
            Rig2DRenderer.DrawAll(spriteBatch, rig, in ctx);
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
    }
}
