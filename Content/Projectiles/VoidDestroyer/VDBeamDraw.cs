using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
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
        public static void Draw(Vector2 start, Vector2 dir, float length, float width, Color color, Color coreColor, float envelope, float opacity, float seed = 0f)
        {
            if (envelope <= 0.001f || opacity <= 0.001f || length <= 1f)
            {
                return;
            }
            dir = dir.SafeNormalize(Vector2.UnitY);
            Effect shader = Shader;
            Vector2 screenStart = start - Main.screenPosition;
            float rot = dir.ToRotation();

            if (shader != null)
            {
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
            else
            {
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
    }
}
