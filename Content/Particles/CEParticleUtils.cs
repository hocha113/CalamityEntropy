using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    /// <summary>
    /// 粒子端口的本地绘制工具:原先调 CalamityUtils 的几处等价实现,脱离灾厄后粒子目录不再引用灾厄
    /// </summary>
    internal static class CEParticleUtils
    {
        //0..1进度映射成0..1..0的正弦鼓包
        internal static float Convert01To010(float value) => (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(value, 0f, 1f));

        //RGB三通道各画一遍,沿direction垂直方向偏移±strength出色差
        internal static void DrawChromaticAberration(Vector2 direction, float strength, Action<Vector2, Color> drawCall) {
            for (int i = -1; i <= 1; i++) {
                Color aberrationColor = i switch {
                    -1 => new Color(255, 0, 0, 0),
                    0 => new Color(0, 255, 0, 0),
                    _ => new Color(0, 0, 255, 0),
                };
                drawCall(direction.RotatedBy(MathHelper.PiOver2) * i * strength, aberrationColor);
            }
        }

        //End+Begin(Immediate)进shader区,静态方法不做扩展,避免和别处同名扩展撞车
        internal static void EnterShaderRegion(SpriteBatch spriteBatch, BlendState newBlendState = null, Effect effect = null) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, newBlendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
