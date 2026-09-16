using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;

namespace CalamityEntropy.Core.Graphics
{
    /// <summary>
    /// 灾厄绘制扩展方法的自有等效实现。方法名与签名与灾厄原版一致, 原调用点只换 using 即可。
    /// 建议后续并入 CEUtils 统一维护(见 primitives-api.md)。
    /// </summary>
    public static class CEShaderExtensions
    {
        /// <summary>
        /// 给 MiscShaderData 设置采样贴图。灾厄原版直写私有字段 _uImageX(靠 publicizer),
        /// 本仓库无 publicizer, 改走 tML 公开的 UseImageX, 效果完全等价。
        /// </summary>
        public static MiscShaderData SetShaderTexture(this MiscShaderData shader, Asset<Texture2D> texture, int index = 1) {
            return index switch {
                0 => shader.UseImage0(texture),
                2 => shader.UseImage2(texture),
                _ => shader.UseImage1(texture),
            };
        }

        /// <summary>切到 Immediate 批次, 使后续 Draw 逐个应用着色器。</summary>
        public static void EnterShaderRegion(this SpriteBatch spriteBatch, BlendState newBlendState = null, Effect effect = null, Matrix? matrix = null) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, newBlendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, matrix ?? Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>回到默认 Deferred/AlphaBlend 批次。</summary>
        public static void ExitShaderRegion(this SpriteBatch spriteBatch, Matrix? matrix = null) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, matrix ?? Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
