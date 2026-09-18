using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 能量射线绘制助手:把一根射线经 VDVoidBeam 着色器画成带湍流、白热核心与边缘辉光的光柱。
    /// 光柱/主炮/红射线共用;着色器缺失时退回 BasicTrail + VoidLaser 的加法叠层,功能不丢
    /// </summary>
    public static class VDBeamDraw
    {
        /// <summary>着色器里 uEnvelope 对应的是四边形半高;四边形比可见宽度宽 2.2 倍给辉光留位</summary>
        private const float QuadWidthMult = 2.2f;

        private static Effect Shader => CEEffectAssets.VDVoidBeam?.Value;

        /// <summary>
        /// 画一根从 start 沿 dir 长 length 的射线。width 为可见宽度(像素),envelope 是宽度包络 0..1,
        /// color 主色、coreColor 核心热色,seed 让并存的多根射线湍流不同步
        /// </summary>
        public static void Draw(Vector2 start, Vector2 dir, float length, float width, Color color, Color coreColor, float envelope, float opacity, float seed = 0f) {
            if (envelope <= 0.001f || opacity <= 0.001f || length <= 1f) {
                return;
            }
            dir = dir.SafeNormalize(Vector2.UnitY);
            Effect shader = Shader;
            Vector2 screenStart = start - Main.screenPosition;
            float rot = dir.ToRotation();

            if (shader != null) {
                Texture2D strip = CEExtraAssets.white ?? CEUtils.getExtraTex("white");
                Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive, shader);
                Main.instance.GraphicsDevice.Textures[1] = noise;
                Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["uColor"]?.SetValue(color.ToVector3());
                shader.Parameters["uColor2"]?.SetValue(coreColor.ToVector3());
                shader.Parameters["uEnvelope"]?.SetValue(envelope / QuadWidthMult);
                shader.Parameters["uLength"]?.SetValue(length / Math.Max(width, 1f));
                shader.Parameters["uOpacity"]?.SetValue(opacity);
                shader.Parameters["uSeed"]?.SetValue(seed);
                shader.CurrentTechnique.Passes[0].Apply();
                Vector2 scale = new Vector2(length / strip.Width, width * QuadWidthMult / strip.Height);
                Main.spriteBatch.Draw(strip, screenStart, null, Color.White, rot, new Vector2(0f, strip.Height / 2f), scale, SpriteEffects.None, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }
            else {
                Texture2D core = CEUtils.getExtraTex("VoidLaser");
                Texture2D glowBeam = CEUtils.getExtraTex("BasicTrail");
                Vector2 mid = screenStart + dir * length * 0.5f;
                float w = width * envelope;
                Main.spriteBatch.UseAdditive();
                Main.spriteBatch.Draw(glowBeam, mid, null, color * (0.6f * opacity), rot, glowBeam.Size() / 2f, new Vector2(length / glowBeam.Width, w * 3.2f / glowBeam.Height), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(core, screenStart, null, color * opacity, rot, new Vector2(0, core.Height / 2f), new Vector2(length / core.Width, w * 1.1f / core.Height), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(core, screenStart, null, coreColor * (0.9f * opacity), rot, new Vector2(0, core.Height / 2f), new Vector2(length / core.Width, w * 0.45f / core.Height), SpriteEffects.None, 0f);
                CEUtils.ReSetToEndShader();
            }

            //起点光球:射线的「枢」
            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            float glowScale = width / 120f * envelope;
            Main.spriteBatch.Draw(glow, screenStart, null, color * (0.9f * opacity), 0f, glow.Size() / 2f, glowScale * 1.6f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, screenStart, null, coreColor * (0.8f * opacity), 0f, glow.Size() / 2f, glowScale * 0.8f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
        }

        /// <summary>
        /// 透视射线:从 start 到 end 的梯形光柱,两端可见宽度分别为 widthStart / widthEnd(像素),
        /// 「远端细、近端粗」就是纵深。四顶点梯形套同一支 VDVoidBeam 着色器(UV 沿梯形插值,可见宽度随之等比收窄);
        /// start / end 都是世界坐标(调用方已投影)。着色器缺失时退回两层折线。进出批次保持 Deferred/AlphaBlend
        /// </summary>
        public static void DrawTapered(Vector2 start, Vector2 end, float widthStart, float widthEnd, Color color, Color coreColor, float envelope, float opacity, float seed = 0f, bool endGlow = true) {
            if (envelope <= 0.001f || opacity <= 0.001f) {
                return;
            }
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 1f) {
                return;
            }
            Vector2 dir = delta / length;
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            Vector2 s = start - Main.screenPosition;
            Vector2 e = end - Main.screenPosition;
            Effect shader = Shader;
            SpriteBatch sb = Main.spriteBatch;

            if (shader != null) {
                Texture2D strip = CEExtraAssets.white ?? CEUtils.getExtraTex("white");
                Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
                float halfS = widthStart * QuadWidthMult * 0.5f;
                float halfE = widthEnd * QuadWidthMult * 0.5f;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                GraphicsDevice gd = Main.instance.GraphicsDevice;
                gd.Textures[0] = strip;
                gd.Textures[1] = noise;
                gd.SamplerStates[1] = SamplerState.LinearWrap;
                shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["uColor"]?.SetValue(color.ToVector3());
                shader.Parameters["uColor2"]?.SetValue(coreColor.ToVector3());
                shader.Parameters["uEnvelope"]?.SetValue(envelope / QuadWidthMult);
                shader.Parameters["uLength"]?.SetValue(length / Math.Max((widthStart + widthEnd) * 0.5f, 1f));
                shader.Parameters["uOpacity"]?.SetValue(opacity);
                shader.Parameters["uSeed"]?.SetValue(seed);
                shader.CurrentTechnique.Passes[0].Apply();
                ColoredVertex[] quad =
                {
                    new ColoredVertex(s - perp * halfS, new Vector3(0f, 0f, 1f), Color.White),
                    new ColoredVertex(s + perp * halfS, new Vector3(0f, 1f, 1f), Color.White),
                    new ColoredVertex(e - perp * halfE, new Vector3(1f, 0f, 1f), Color.White),
                    new ColoredVertex(e + perp * halfE, new Vector3(1f, 1f, 1f), Color.White),
                };
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, quad, 0, 2);
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            else {
                sb.UseAdditive();
                //退化:沿线分 6 段、宽度线性插值的折线
                const int segs = 6;
                for (int i = 0; i < segs; i++) {
                    float t0 = i / (float)segs;
                    float t1 = (i + 1) / (float)segs;
                    float w = MathHelper.Lerp(widthStart, widthEnd, (t0 + t1) * 0.5f) * envelope;
                    Vector2 a = start + delta * t0;
                    Vector2 b = start + delta * t1;
                    CEUtils.drawLineBetter(a, b, color * (0.7f * opacity), w * 1.6f, 2);
                    CEUtils.drawLine(a, b, coreColor * (0.8f * opacity), Math.Max(w * 0.35f, 1f), 2);
                }
                CEUtils.ReSetToEndShader();
            }

            if (endGlow) {
                //粗端光球:光柱的「枢」在粗的那一端
                sb.UseAdditive();
                Texture2D glow = CEUtils.getExtraTex("Glow");
                bool startThick = widthStart >= widthEnd;
                Vector2 hub = startThick ? s : e;
                float glowScale = Math.Max(widthStart, widthEnd) / 120f * envelope;
                sb.Draw(glow, hub, null, color * (0.8f * opacity), 0f, glow.Size() / 2f, glowScale * 1.5f, SpriteEffects.None, 0f);
                sb.Draw(glow, hub, null, coreColor * (0.7f * opacity), 0f, glow.Size() / 2f, glowScale * 0.7f, SpriteEffects.None, 0f);
                CEUtils.ReSetToEndShader();
            }
        }

        #region 批量透视射线
        private static Effect batchShader;
        private static readonly ColoredVertex[] batchQuad = new ColoredVertex[4];

        /// <summary>
        /// 批量透视射线:一次进入着色器批次,之后逐根 <see cref="TaperedQuad"/>,最后 <see cref="EndTapered"/> 恢复 Deferred/AlphaBlend。
        /// 同一批共用配色 / 不透明度 / 种子(点阵那种几十根同色射线用它,免得每根切一次批次)。
        /// 返回 false 表示着色器缺失,调用方改用逐根 <see cref="DrawTapered"/>(内部有折线退化)
        /// </summary>
        public static bool BeginTapered(Color color, Color coreColor, float opacity, float seed = 0f) {
            Effect shader = Shader;
            if (shader == null) {
                return false;
            }
            batchShader = shader;
            Texture2D strip = CEExtraAssets.white ?? CEUtils.getExtraTex("white");
            Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.Textures[0] = strip;
            gd.Textures[1] = noise;
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uColor"]?.SetValue(color.ToVector3());
            shader.Parameters["uColor2"]?.SetValue(coreColor.ToVector3());
            shader.Parameters["uOpacity"]?.SetValue(opacity);
            shader.Parameters["uSeed"]?.SetValue(seed);
            return true;
        }

        /// <summary>批次内画一根梯形射线(start / end 世界坐标,已投影);宽度包络与长度参数逐根重设并 Apply,不切批次</summary>
        public static void TaperedQuad(Vector2 start, Vector2 end, float widthStart, float widthEnd, float envelope) {
            if (batchShader == null || envelope <= 0.001f) {
                return;
            }
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 1f) {
                return;
            }
            Vector2 dir = delta / length;
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            Vector2 s = start - Main.screenPosition;
            Vector2 e = end - Main.screenPosition;
            float halfS = widthStart * QuadWidthMult * 0.5f;
            float halfE = widthEnd * QuadWidthMult * 0.5f;
            batchShader.Parameters["uEnvelope"]?.SetValue(envelope / QuadWidthMult);
            batchShader.Parameters["uLength"]?.SetValue(length / Math.Max((widthStart + widthEnd) * 0.5f, 1f));
            batchShader.CurrentTechnique.Passes[0].Apply();
            batchQuad[0] = new ColoredVertex(s - perp * halfS, new Vector3(0f, 0f, 1f), Color.White);
            batchQuad[1] = new ColoredVertex(s + perp * halfS, new Vector3(0f, 1f, 1f), Color.White);
            batchQuad[2] = new ColoredVertex(e - perp * halfE, new Vector3(1f, 0f, 1f), Color.White);
            batchQuad[3] = new ColoredVertex(e + perp * halfE, new Vector3(1f, 1f, 1f), Color.White);
            Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, batchQuad, 0, 2);
        }

        public static void EndTapered() {
            batchShader = null;
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        #endregion
    }
}
