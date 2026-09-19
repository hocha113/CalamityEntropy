using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 能量射线绘制助手。直射线(<see cref="Draw"/>)经 VDVoidBeam 着色器画在一张沿射线拉伸的白条上;
    /// 透视射线(<see cref="DrawTapered"/> 与 <see cref="BeginTapered"/> / <see cref="TaperedQuad"/> / <see cref="EndTapered"/> 三件套)
    /// 经 VDBeamTapered 着色器画成四顶点梯形:顶点纹理坐标是像素单位的「沿轴 / 横向」仿射量,跨三角剖分精确插值,
    /// 没有归一化 UV 梯形在两三角形共享对角线处的中线折断(那条折断偏 (hS - hE) / 2 像素,红射线发射期约 94px);
    /// 两端可选传 Z,得到透视校正的噪声压缩与远端雾化变暗。着色器缺失时都退回加法贴图叠层,功能不丢
    /// </summary>
    public static class VDBeamDraw
    {
        /// <summary>着色器里 uEnvelope 对应的是四边形半高;四边形比可见宽度宽 2.2 倍给辉光留位</summary>
        private const float QuadWidthMult = 2.2f;
        /// <summary>端帽默认渐隐比例(与 VDVoidBeam 内置的一致):起点 4%,终点 8%</summary>
        public const float DefaultCapStart = 0.04f;
        public const float DefaultCapEnd = 0.08f;
        /// <summary>端帽下限:着色器里 smoothstep(0, 0, t) 是除零,顶进「枢」的那端也留这么一丁点</summary>
        private const float CapMin = 0.002f;

        private static Effect Shader => CEEffectAssets.VDVoidBeam?.Value;
        private static Effect TaperedShader => CEEffectAssets.VDBeamTapered?.Value;

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

        #region 透视射线
        private static readonly ColoredVertex[] quad = new ColoredVertex[4];
        private static ColoredVertex[] fallbackStrip;
        private static Effect batchShader;

        /// <summary>
        /// 透视射线:从 start 到 end 的梯形光柱,两端可见宽度分别为 widthStart / widthEnd(像素),「远端细、近端粗」就是纵深。
        /// start / end 都是世界坐标(调用方已投影);zStart / zEnd 是两端的 Z(默认 0 = 纯平面梯形,只修折断不加纵深线索):
        /// 传了 Z 就按透视校正铺噪声(远端压缩)、按 VDDepth 雾化与变暗远端;镜头前(Z &lt; 0)那端不衰减,光锥是打进画面里的光。
        /// capStart / capEnd 是两端端帽渐隐比例,顶进「枢」的那端传 0。进出批次保持 Deferred/AlphaBlend
        /// </summary>
        public static void DrawTapered(Vector2 start, Vector2 end, float widthStart, float widthEnd, Color color, Color coreColor, float envelope, float opacity, float seed = 0f, bool endGlow = true,
            float zStart = 0f, float zEnd = 0f, float capStart = DefaultCapStart, float capEnd = DefaultCapEnd) {
            if (envelope <= 0.001f || opacity <= 0.001f) {
                return;
            }
            Vector2 delta = end - start;
            if (delta.Length() <= 1f) {
                return;
            }
            Effect shader = TaperedShader;
            SpriteBatch sb = Main.spriteBatch;

            if (shader != null) {
                sb.End();
                BeginTaperedBatch(sb, shader);
                ApplyShared(shader, color, coreColor, opacity, seed);
                DrawQuad(shader, start, end, widthStart, widthEnd, envelope, zStart, zEnd, capStart, capEnd);
                EndTaperedBatch(sb);
            }
            else {
                DrawTaperedFallback(start, end, widthStart, widthEnd, color, coreColor, envelope, opacity);
            }

            if (endGlow) {
                //粗端光球:光柱的「枢」在粗的那一端
                sb.UseAdditive();
                Texture2D glow = CEUtils.getExtraTex("Glow");
                bool startThick = widthStart >= widthEnd;
                Vector2 hub = (startThick ? start : end) - Main.screenPosition;
                float glowScale = Math.Max(widthStart, widthEnd) / 120f * envelope;
                sb.Draw(glow, hub, null, color * (0.8f * opacity), 0f, glow.Size() / 2f, glowScale * 1.5f, SpriteEffects.None, 0f);
                sb.Draw(glow, hub, null, coreColor * (0.7f * opacity), 0f, glow.Size() / 2f, glowScale * 0.7f, SpriteEffects.None, 0f);
                CEUtils.ReSetToEndShader();
            }
        }

        /// <summary>
        /// 批量透视射线:一次进入着色器批次,之后逐根 <see cref="TaperedQuad"/>,最后 <see cref="EndTapered"/> 恢复 Deferred/AlphaBlend。
        /// 同一批共用配色 / 不透明度 / 种子(点阵那种几十根同色射线用它,免得每根切一次批次)。
        /// 返回 false 表示着色器缺失,调用方改用逐根 <see cref="DrawTapered"/>(内部有退化)
        /// </summary>
        public static bool BeginTapered(Color color, Color coreColor, float opacity, float seed = 0f) {
            Effect shader = TaperedShader;
            if (shader == null) {
                return false;
            }
            batchShader = shader;
            Main.spriteBatch.End();
            BeginTaperedBatch(Main.spriteBatch, shader);
            ApplyShared(shader, color, coreColor, opacity, seed);
            return true;
        }

        /// <summary>批次内画一根梯形射线(start / end 世界坐标,已投影;Z 与端帽语义同 <see cref="DrawTapered"/>);逐根重设梯形与纵深参数并 Apply,不切批次</summary>
        public static void TaperedQuad(Vector2 start, Vector2 end, float widthStart, float widthEnd, float envelope,
            float zStart = 0f, float zEnd = 0f, float capStart = DefaultCapStart, float capEnd = DefaultCapEnd) {
            if (batchShader == null || envelope <= 0.001f) {
                return;
            }
            DrawQuad(batchShader, start, end, widthStart, widthEnd, envelope, zStart, zEnd, capStart, capEnd);
        }

        public static void EndTapered() {
            batchShader = null;
            EndTaperedBatch(Main.spriteBatch);
        }

        /// <summary>进入梯形着色器批次(调用方已 End):白条绑 s0 只为批次状态齐整,噪声绑 s1</summary>
        private static void BeginTaperedBatch(SpriteBatch sb, Effect shader) {
            Texture2D strip = CEExtraAssets.white ?? CEUtils.getExtraTex("white");
            Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.Textures[0] = strip;
            gd.Textures[1] = noise;
            gd.SamplerStates[1] = SamplerState.LinearWrap;
        }

        private static void EndTaperedBatch(SpriteBatch sb) {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>一批共用的参数:时间 / 配色 / 不透明度 / 种子 / 雾色</summary>
        private static void ApplyShared(Effect shader, Color color, Color coreColor, float opacity, float seed) {
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uColor"]?.SetValue(color.ToVector3());
            shader.Parameters["uColor2"]?.SetValue(coreColor.ToVector3());
            shader.Parameters["uOpacity"]?.SetValue(opacity);
            shader.Parameters["uSeed"]?.SetValue(seed);
            shader.Parameters["uFogColor"]?.SetValue(VDVfx.FarFog.ToVector3());
        }

        /// <summary>
        /// 逐根:算几何、喂梯形与纵深参数、Apply、画四顶点 TriangleStrip。
        /// 顶点纹理坐标 = (沿轴像素, 到轴线的有符号横向像素),两者都是屏幕位置的线性函数,着色器里再归一化,所以对角线两侧插值一致
        /// </summary>
        private static void DrawQuad(Effect shader, Vector2 start, Vector2 end, float widthStart, float widthEnd, float envelope, float zStart, float zEnd, float capStart, float capEnd) {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 1f) {
                return;
            }
            Vector2 dir = delta / length;
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            Vector2 s = start - Main.screenPosition;
            Vector2 e = end - Main.screenPosition;
            float halfS = Math.Max(widthStart, 0.5f) * QuadWidthMult * 0.5f;
            float halfE = Math.Max(widthEnd, 0.5f) * QuadWidthMult * 0.5f;
            float wS = VDDepth.W(zStart);
            float wE = VDDepth.W(zEnd);
            //噪声瓦片数按世界长宽比铺:屏幕长 × 平均透视权重 ≈ 世界长,一格噪声约一个射线宽
            float avgWidth = Math.Max((widthStart + widthEnd) * 0.5f, 1f);
            float tile = length * (wS + wE) * 0.5f / avgWidth;

            shader.Parameters["uLengthPx"]?.SetValue(length);
            shader.Parameters["uHalfStart"]?.SetValue(halfS);
            shader.Parameters["uHalfEnd"]?.SetValue(halfE);
            shader.Parameters["uEnvelope"]?.SetValue(envelope / QuadWidthMult);
            shader.Parameters["uWStart"]?.SetValue(wS);
            shader.Parameters["uWEnd"]?.SetValue(wE);
            shader.Parameters["uTile"]?.SetValue(tile);
            shader.Parameters["uFogStart"]?.SetValue(VDDepth.FogAmount(zStart) * VDDirector.BeamFogMult);
            shader.Parameters["uFogEnd"]?.SetValue(VDDepth.FogAmount(zEnd) * VDDirector.BeamFogMult);
            shader.Parameters["uAlphaStart"]?.SetValue(DepthAlpha(zStart));
            shader.Parameters["uAlphaEnd"]?.SetValue(DepthAlpha(zEnd));
            shader.Parameters["uCap"]?.SetValue(new Vector2(Math.Max(capStart, CapMin), Math.Max(capEnd, CapMin)));
            shader.CurrentTechnique.Passes[0].Apply();

            quad[0] = new ColoredVertex(s - perp * halfS, new Vector3(0f, -halfS, 1f), Color.White);
            quad[1] = new ColoredVertex(s + perp * halfS, new Vector3(0f, halfS, 1f), Color.White);
            quad[2] = new ColoredVertex(e - perp * halfE, new Vector3(length, -halfE, 1f), Color.White);
            quad[3] = new ColoredVertex(e + perp * halfE, new Vector3(length, halfE, 1f), Color.White);
            Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quad, 0, 2);
        }

        /// <summary>射线一端的深度透明度:远端按 VDDepth.Alpha 变暗(留 55% 地板);镜头前(Z &lt; 0)固定 1,光锥不随近景剪影衰减</summary>
        private static float DepthAlpha(float z) => z > 0f ? VDDepth.Alpha(z) : 1f;

        /// <summary>
        /// 退化(着色器缺失):沿轴分 BeamFallbackSegments 段的 TriangleStrip,BasicTrail 软边条贴图走原版精灵着色器加法叠两层(宽软晕 + 窄核心)。
        /// 分段后每段的仿射误差只有总宽差的 1/16,中线不再折断;不再是 6 段阶梯折线
        /// </summary>
        private static void DrawTaperedFallback(Vector2 start, Vector2 end, float widthStart, float widthEnd, Color color, Color coreColor, float envelope, float opacity) {
            Texture2D glowBeam = CEExtraAssets.BasicTrail ?? CEUtils.getExtraTex("BasicTrail");
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.Textures[0] = glowBeam;
            DrawFallbackStrip(gd, start, end, widthStart * 1.6f * envelope, widthEnd * 1.6f * envelope, color * (0.7f * opacity));
            DrawFallbackStrip(gd, start, end, widthStart * 0.5f * envelope, widthEnd * 0.5f * envelope, coreColor * (0.9f * opacity));
            EndTaperedBatch(sb);
        }

        private static void DrawFallbackStrip(GraphicsDevice gd, Vector2 start, Vector2 end, float widthStart, float widthEnd, Color color) {
            int segs = Math.Max(VDDirector.BeamFallbackSegments, 1);
            int count = (segs + 1) * 2;
            if (fallbackStrip == null || fallbackStrip.Length != count) {
                fallbackStrip = new ColoredVertex[count];
            }
            Vector2 delta = end - start;
            Vector2 dir = delta.SafeNormalize(Vector2.UnitY);
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            for (int i = 0; i <= segs; i++) {
                float t = i / (float)segs;
                Vector2 p = start + delta * t - Main.screenPosition;
                float half = Math.Max(MathHelper.Lerp(widthStart, widthEnd, t), 1f) * 0.5f;
                fallbackStrip[i * 2] = new ColoredVertex(p - perp * half, new Vector3(t, 0f, 1f), color);
                fallbackStrip[i * 2 + 1] = new ColoredVertex(p + perp * half, new Vector3(t, 1f, 1f), color);
            }
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, fallbackStrip, 0, count - 2);
        }
        #endregion
    }
}
