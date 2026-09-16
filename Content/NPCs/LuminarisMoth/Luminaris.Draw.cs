using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth
{
    /// <summary>
    /// 月华之蛾的绘制层。全部是本地推导:
    /// 残影窗口、尾迹采样、大尾迹强度都由上下文提供,而它们只由已过线的状态号与出招倒计时决定。
    /// <para>
    /// 本体与尾巴读的是<b>同一层</b>位置:AI 开头 <c>CEBossNetMotion.BeginFrame</c> 已把
    /// <c>netOffset</c> 清零,绳根又是在 AI 里按 <c>NPC.Center</c> 推进的,所以两者都画在裸中心上,不会出现接缝
    /// </para>
    /// </summary>
    public partial class Luminaris
    {
        //尾巴与星光贴图改由 VaultLoaden 在加载期赋值,卸载自动置空;texture 是 NPC 本体贴图(tML 自管),保留原懒取
        public static Texture2D texture = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/LuminarisMoth/t1")]
        public static Texture2D texTail1;
        [VaultLoaden("CalamityEntropy/Content/NPCs/LuminarisMoth/t2")]
        public static Texture2D texTail2;

        public override void Unload()
        {
            texture = null;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (texture == null)
            {
                texture = NPC.getTexture();
            }

            List<Vector2> trail = Context?.Trail;
            int afterImageTime = Context?.AfterImageTime ?? 0;
            List<Vector2> afterImagePoints = new List<Vector2>();
            if (afterImageTime > 0 && trail != null && trail.Count > 8)
            {
                //只取尾迹的后半段,每段再细分五份,画成一串越旧越淡的本体
                for (int i = trail.Count - 1; i > trail.Count / 2; i--)
                {
                    for (float j = 0; j < 1; j += 0.2f)
                    {
                        afterImagePoints.Add(Vector2.Lerp(trail[i], trail[i - 1], j));
                    }
                }
                for (int i = 0; i < afterImagePoints.Count; i++)
                {
                    DrawMyself(afterImagePoints[i], Color.White * (1 - ((i + 1f) / afterImagePoints.Count)) * 0.16f * (afterImageTime / 16f), true);
                }
            }

            DrawMyself(NPC.Center, Color.White);

            return false;
        }

        public void DrawMyself(Vector2 pos, Color color, bool afterImage = false)
        {
            DrawTails(pos - NPC.Center, color);
            int phase = Context?.Phase ?? 1;
            if (!afterImage)
            {
                Asset<Texture2D> textured = CEExtraAssets.EnchantedAsset;
                Effect shader = CEEffectAssets.Transform3;
                shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.2f);
                shader.Parameters["color"].SetValue((phase == 1 ? new Color(0, 190, 250, 255) : new Color(160, 80, 255, 255)).ToVector4());
                shader.Parameters["strength"].SetValue(phase == 1 ? 0.2f : 1f);

                shader.CurrentTechnique.Passes["EnchantedPass"].Apply();
                Main.instance.GraphicsDevice.Textures[1] = textured.Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(0, Main.spriteBatch.GraphicsDevice.BlendState, Main.spriteBatch.GraphicsDevice.SamplerStates[0], Main.spriteBatch.GraphicsDevice.DepthStencilState, Main.spriteBatch.GraphicsDevice.RasterizerState, shader, Main.Transform);
            }
            Rectangle frame = new Rectangle(0, (texture.Height / Main.npcFrameCount[Type]) * ((frameCounter / 4) % Main.npcFrameCount[Type]), texture.Width, (texture.Height / Main.npcFrameCount[Type]) - 2);
            Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color * NPC.Opacity, NPC.rotation, new Vector2(texture.Width / 2, 104), NPC.scale, SpriteEffects.None);

            if (!afterImage)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                float starX = 1f + (float)Math.Cos(Main.GlobalTimeWrappedHourly * 26) * 0.4f;
                Vector2 starScale = new Vector2(starX, starX) * (1 + (Context?.MegaTrail ?? 0f) * 1.6f);
                Texture2D texStar = CEExtraAssets.StarTexture_White;
                Main.spriteBatch.Draw(texStar, pos - Main.screenPosition, null, Color.LightBlue * (color.A / 255f) * NPC.Opacity, 0, texStar.Size() * 0.5f, new Vector2(1f, 0.8f * 0.7f) * starScale * NPC.scale * 0.74f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(texStar, pos - Main.screenPosition, null, Color.LightBlue * (color.A / 255f) * NPC.Opacity, 0, texStar.Size() * 0.5f, new Vector2(0.8f, 1f * 0.7f) * starScale * NPC.scale * 0.74f, SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
                drawT();
            }
        }

        /// <summary>
        /// 尾迹的四层图元:常态两层(天蓝底 20 宽 + 白 16 宽),大尾迹亮着时再叠两层
        /// (对采样点做 5 倍插值加密,70 / 64 宽)。
        /// <para>
        /// 开头临时补一个当前中心、末尾再摘掉:采样是在 AI 末尾做的,绘制时本体已经又往前走了一帧,
        /// 补这一个点尾迹才接得上本体。原代码的加减是配对的,所以对 AI 侧的上限裁剪没有净影响
        /// </para>
        /// </summary>
        public void drawT()
        {
            List<Vector2> odp = Context?.Trail;
            if (odp == null)
            {
                return;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            odp.Add(NPC.Center);
            if (odp.Count > 2)
            {
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    Color b = Color.SkyBlue * NPC.Opacity;

                    float a = 0;
                    for (int i = 1; i < odp.Count; i++)
                    {
                        a += 1f / (float)odp.Count;

                        ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 20 * ((i - 1f) / (odp.Count - 2f)),
                              new Vector3((float)(i + 1) / odp.Count + Main.GlobalTimeWrappedHourly, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 20 * ((i - 1f) / (odp.Count - 2f)),
                              new Vector3((float)(i + 1) / odp.Count + Main.GlobalTimeWrappedHourly, 0, 1),
                              b * a));
                    }

                    if (ve.Count >= 3)
                    {
                        Texture2D tx = CEExtraAssets.MegaStreakBacking2;
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    Color b = Color.White * NPC.Opacity;

                    float a = 0;
                    for (int i = 1; i < odp.Count; i++)
                    {
                        a += 1f / (float)odp.Count;

                        ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * ((i - 1f) / (odp.Count - 2f)),
                              new Vector3((float)(i + 1) / odp.Count + Main.GlobalTimeWrappedHourly, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * ((i - 1f) / (odp.Count - 2f)),
                              new Vector3((float)(i + 1) / odp.Count + Main.GlobalTimeWrappedHourly, 0, 1),
                              b * a));
                    }

                    if (ve.Count >= 3)
                    {
                        Texture2D tx = CEExtraAssets.Streak1;
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                if ((Context?.MegaTrail ?? 0f) > 0)
                {
                    float megaTrail = Context.MegaTrail;
                    {
                        List<ColoredVertex> ve = new List<ColoredVertex>();
                        Color b = Color.SkyBlue * NPC.Opacity * megaTrail;
                        var ptd = CEUtils.WrapPoints(odp, 5);
                        float a = 0;
                        for (int i = 1; i < ptd.Count; i++)
                        {
                            a += 1f / (float)ptd.Count;
                            ve.Add(new ColoredVertex(ptd[i] - Main.screenPosition + (ptd[i] - ptd[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 70 * ((i - 1f) / (ptd.Count - 2f)),
                                  new Vector3((float)(i + 1) / ptd.Count + Main.GlobalTimeWrappedHourly, 1, 1),
                                b * a));
                            ve.Add(new ColoredVertex(ptd[i] - Main.screenPosition + (ptd[i] - ptd[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 70 * ((i - 1f) / (ptd.Count - 2f)),
                                  new Vector3((float)(i + 1) / ptd.Count + Main.GlobalTimeWrappedHourly, 0, 1),
                                  b * a));
                        }

                        if (ve.Count >= 3)
                        {
                            Texture2D tx = CEExtraAssets.MegaStreakInner;
                            gd.Textures[0] = tx;
                            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                        }
                    }
                    {
                        List<ColoredVertex> ve = new List<ColoredVertex>();
                        Color b = Color.White * NPC.Opacity * megaTrail;
                        var ptd = CEUtils.WrapPoints(odp, 5);
                        float a = 0;
                        for (int i = 1; i < ptd.Count; i++)
                        {
                            a += 1f / (float)ptd.Count;
                            ve.Add(new ColoredVertex(ptd[i] - Main.screenPosition + (ptd[i] - ptd[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 64 * ((i - 1f) / (ptd.Count - 2f)),
                                  new Vector3((float)(i + 1) / ptd.Count + Main.GlobalTimeWrappedHourly, 1, 1),
                                b * a));
                            ve.Add(new ColoredVertex(ptd[i] - Main.screenPosition + (ptd[i] - ptd[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 64 * ((i - 1f) / (ptd.Count - 2f)),
                                  new Vector3((float)(i + 1) / ptd.Count + Main.GlobalTimeWrappedHourly, 0, 1),
                                  b * a));
                        }

                        if (ve.Count >= 3)
                        {
                            Texture2D tx = CEExtraAssets.Streak2;
                            gd.Textures[0] = tx;
                            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                        }
                    }
                }
            }
            odp.RemoveAt(odp.Count - 1);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #region drawTail
        public void DrawTails(Vector2 pos, Color color)
        {
            GraphicsDevice gd = Main.spriteBatch.GraphicsDevice;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (tail1 != null)
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = color * NPC.Opacity;
                List<Vector2> tailPoints = tail1.GetPoints();
                for (int i = 1; i < tailPoints.Count; i++)
                {
                    ve.Add(new ColoredVertex(tailPoints[i] + pos - Main.screenPosition + (tailPoints[i] - tailPoints[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18,
                          new Vector3((float)(i + 1) / tailPoints.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(tailPoints[i] + pos - Main.screenPosition + (tailPoints[i] - tailPoints[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18,
                          new Vector3((float)(i + 1) / tailPoints.Count, 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    Texture2D tx = texTail1;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            if (tail2 != null)
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = color * NPC.Opacity;
                List<Vector2> tailPoints = tail2.GetPoints();
                for (int i = 1; i < tailPoints.Count; i++)
                {
                    ve.Add(new ColoredVertex(tailPoints[i] + pos - Main.screenPosition + (tailPoints[i] - tailPoints[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18,
                          new Vector3((float)(i + 1) / tailPoints.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(tailPoints[i] + pos - Main.screenPosition + (tailPoints[i] - tailPoints[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18,
                          new Vector3((float)(i + 1) / tailPoints.Count, 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    Texture2D tx = texTail2;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.ExitShaderRegion();
        }
        #endregion
    }
}
