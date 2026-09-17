using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using static CalamityEntropy.CalamityEntropy;

namespace CalamityEntropy.Core.Graphics.Screen
{
    /// <summary>
    /// 演出层的三件全屏东西:命中闪光的泛光、斩击切屏、以及压暗整屏的黑幕。
    /// <para>
    /// 前两者靠拷屏与模糊着色器,只在 RT 可用时跑;
    /// <see cref="DrawBlackMask"/> 只是画一个整屏矩形,不需要任何 RT,两条路径都会调它。
    /// </para>
    /// <para>三者的推进量都在 <c>EModSys.PostUpdateDusts</c> 里按游戏帧走,不依赖渲染帧</para>
    /// </summary>
    internal static class CECinematicScreen
    {
        /// <summary>命中闪光:把整屏按 16 层逐级放大叠加,做出一次过曝</summary>
        public static void DrawFlashBloom(GraphicsDevice graphicsDevice) {
            if (FlashEffectStrength <= 0) {
                return;
            }

            RenderTarget2D screen0 = CEScreenPipeline.Screen0;
            if (screen0 == null) {
                return;
            }
            CEScreenPipeline.CaptureScreenTo(graphicsDevice, screen0);

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            for (float i = 1; i <= 16; i++) {
                Main.spriteBatch.Draw(screen0, screen0.Size() / 2, null, Color.White * ((16f / i) * 0.1f * FlashEffectStrength), 0, screen0.Size() / 2, 1 + FlashEffectStrength * 0.08f * i, SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
            //Immediate 模式下 Begin 会立刻写设备状态,这对空批次是把混合状态复位回 AlphaBlend 用的,不是冗余
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 斩击切屏:沿切线把整屏一分为二,两半朝相反方向错开并加高斯模糊。
        /// 两半各自需要一块完整的屏幕备份,所以吃 Screen0 与 Screen1 两个槽
        /// </summary>
        public static void DrawCutScreen(GraphicsDevice graphicsDevice) {
            if (cutScreen <= 0) {
                return;
            }
            if (!Config.Instance.ScreenWarpEffects) {
                return;
            }
            if (CEScreenPipeline.Screen0 == null || CEScreenPipeline.Screen1 == null) {
                return;
            }

            CEScreenPipeline.CaptureScreenTo(graphicsDevice, CEScreenPipeline.Screen0);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            CEUtils.drawLine(cutScreenCenter, cutScreenCenter + cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 9000, Color.Black, 9000);
            Main.spriteBatch.End();

            CEScreenPipeline.CaptureScreenTo(graphicsDevice, CEScreenPipeline.Screen1);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            CEUtils.drawLine(cutScreenCenter, cutScreenCenter + cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * -9000, Color.Black, 9000);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Black);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Effect blur = EffectLoader.BlurShader;
            blur.CurrentTechnique = blur.Techniques["GaussianBlur"];
            blur.Parameters["resolution"].SetValue(Main.ScreenSize.ToVector2());
            blur.Parameters["blurAmount"].SetValue(cutScreen * 0.036f);
            blur.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * -cutScreen * Main.GameViewMatrix.Zoom.X, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(CEScreenPipeline.Screen1, cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * cutScreen * Main.GameViewMatrix.Zoom.X, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 压暗整屏的黑幕。只画一个整屏矩形,画在当前绑定的目标上,
        /// 所以复古 / 迷幻光照下也能正常工作,由 <see cref="CEScreenPipeline"/> 的兜底位照常调用
        /// </summary>
        public static void DrawBlackMask() {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(CEUtils.pixelTex, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * 0.5f * blackMaskAlpha);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 黑幕淡入淡出的推进。原先写在绘制函数里,按渲染帧走,复古光照下会连带冻结,
        /// 现在由 <c>EModSys.PostUpdateDusts</c> 按游戏帧调用
        /// </summary>
        public static void UpdateBlackMask() {
            if (blackMaskTime > 0) {
                blackMaskAlpha = Math.Min(blackMaskAlpha + 0.05f, 1f);
            }
            else {
                blackMaskAlpha = Math.Max(blackMaskAlpha - 0.025f, 0f);
            }
        }
    }
}
