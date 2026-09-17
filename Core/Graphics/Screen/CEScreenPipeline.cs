using CalamityEntropy.Common;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Core.Graphics.Screen
{
    /// <summary>
    /// 全屏特效管线的唯一编排者:持有 <see cref="RenderHandle"/> 身份与四块自有屏幕 RT,
    /// 按固定顺序调用各情景类,并对外提供「RT 管线本帧能不能用」的门控。
    /// <para>
    /// 具体画什么全在 <see cref="CEVoidScreen"/> / <see cref="CEAbyssScreen"/> / <see cref="CEPixelScreen"/> /
    /// <see cref="CEWarpScreen"/> / <see cref="CECinematicScreen"/> / <see cref="CEEntityOverlay"/> 里,
    /// 这里只管时机、顺序与退让。
    /// </para>
    /// <para>
    /// 复古 / 迷幻光照下原版根本不捕获主画面:<c>Main.DoDraw</c> 的捕获门带 <c>Lighting.NotRetro</c>,
    /// 且 <c>drawToScreen = Lighting.UpdateEveryFrame</c> 会直接释放 <see cref="Main.screenTarget"/>。
    /// 于是 <see cref="EndCaptureDraw"/> 整条不触发,全屏 shader 通道本帧全部让位,
    /// 只有不依赖 RT 的实体叠加改由 <see cref="DrawBeforeInfernoRings"/> 补画。
    /// </para>
    /// </summary>
    internal class CEScreenPipeline : RenderHandle
    {
        public const int MaxScreenSlot = 4;

        public override int ScreenSlot => MaxScreenSlot;

        public static CEScreenPipeline This { get; private set; }

        public static RenderTarget2D Screen0 => Slot(0);
        public static RenderTarget2D Screen1 => Slot(1);
        public static RenderTarget2D Screen2 => Slot(2);
        public static RenderTarget2D Screen3 => Slot(3);

        /// <summary>
        /// 如果没有必要，尽量避免直接访问这个屏幕中间值，可能会影响到与其他模组的交互效果，
        /// 推荐在 <see cref="EndCaptureDraw"/> 通过参数 screenSwap 使用它
        /// </summary>
        public static RenderTarget2D StaticScreenSwap => RenderHandleLoader.ScreenSwap;

        /// <summary>
        /// 本帧 RT 全屏管线是否可用。复古 / 迷幻光照、全屏地图、主菜单下一律为假,
        /// 此时任何读写 <see cref="Main.screenTarget"/> 或 <see cref="Screen0"/> 的通道都必须自行跳过。
        /// <para>
        /// 前四项逐条对应 <c>Main.DoDraw</c> 里决定是否 <c>BeginCapture</c> 的那个判断:
        /// <c>!(drawToScreen || netMode == 2 || worldGen) &amp;&amp; !mapFullscreen &amp;&amp; Lighting.NotRetro</c>。
        /// 其中 <c>drawToScreen</c> 由 <c>Lighting.UpdateEveryFrame</c> 决定,为真时原版已经
        /// <c>ReleaseTargets()</c> 释放了 <see cref="Main.screenTarget"/>,所以额外判一次未释放
        /// </para>
        /// </summary>
        public static bool RTPipelineAvailable => !Main.gameMenu && !Main.mapFullscreen
            && Lighting.NotRetro && !Main.drawToScreen
            && Main.screenTarget != null && !Main.screenTarget.IsDisposed
            && Screen0 != null;

        /// <summary>
        /// 像素通道是否会在本帧绘制。除 RT 可用性外还要看玩家的绚丽特效开关。
        /// 那些「自己不画、等管线代画」的绘制点应当读这个,而不是直接读配置项
        /// </summary>
        public static bool PixelPassActive => RTPipelineAvailable && Config.Instance.EnablePixelEffect;

        public override void Load() => This = this;

        private static RenderTarget2D Slot(int index) {
            RenderTarget2D[] targets = This?.ScreenTargets;
            if (targets == null || index >= targets.Length) {
                return null;
            }
            RenderTarget2D target = targets[index];
            return target != null && !target.IsDisposed ? target : null;
        }

        /// <summary>
        /// 把当前 <see cref="Main.screenTarget"/> 原样拷进 <paramref name="dest"/>,返回后 <paramref name="dest"/> 保持绑定。
        /// 各情景类的「先备份整屏」前置动作统一走这里
        /// </summary>
        public static void CaptureScreenTo(GraphicsDevice graphicsDevice, RenderTarget2D dest) {
            graphicsDevice.SetRenderTarget(dest);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        public static void CaptureScreenTo0(GraphicsDevice graphicsDevice) => CaptureScreenTo(graphicsDevice, Screen0);

        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (!PixelPassActive) {
                return;
            }

            //顺序即历史顺序,任何一段的 RT 绑定都是下一段的前置条件,不要重排
            CaptureScreenTo0(graphicsDevice);

            //虚空:遮罩攒进交换缓冲,再由 cvoid 家族合成回主屏
            CEVoidScreen.DrawVoidMasks(graphicsDevice);
            CEVoidScreen.DrawVoidParticles(graphicsDevice);
            CEVoidScreen.ApplyVoidBackground(graphicsDevice);
            CEVoidScreen.DrawNonPixVoid(graphicsDevice);

            //深渊与血色:各自一套遮罩 + 全屏着色
            CEAbyssScreen.DrawAbyssal(graphicsDevice);
            CEAbyssScreen.DrawBlood(graphicsDevice);

            CEVoidScreen.DrawVoidStarWake(graphicsDevice);

            //像素通道:实体与 IPixelPassPRT 画进 Screen2,再整块过 Pixel shader
            CEPixelScreen.PreparePixelShader(graphicsDevice);
            CEPixelScreen.DrawPixelPassContents();
            CEPixelScreen.ApplyPixelShader(graphicsDevice);

            //无 shader 的实体叠加。复古下这一段由 DrawBeforeInfernoRings 接管
            CEEntityOverlay.DrawScreenOverlay(graphicsDevice);

            //扭曲:刀光与区域碎裂
            CEWarpScreen.DrawSlashWarp(graphicsDevice);
            CEWarpScreen.DrawFragWarp(graphicsDevice);

            //演出层:闪光泛光 → 晚于全部扭曲的实体重绘 → 切屏 → 黑幕
            CECinematicScreen.DrawFlashBloom(graphicsDevice);
            CEEntityOverlay.DrawLateOverlay();
            CECinematicScreen.DrawCutScreen(graphicsDevice);
            CECinematicScreen.DrawBlackMask();
        }

        /// <summary>
        /// RT 管线不可用时的兜底位:该阶段挂在原版 <c>DrawInfernoRings</c> 之前,不依赖画面捕获,
        /// z 序也最接近 EndCapture。只补画不需要 RT 的那些内容,全屏 shader 一概不做近似。
        /// <para>契约:进入时 SpriteBatch 活跃,返回前必须重新开批</para>
        /// </summary>
        public override void DrawBeforeInfernoRings(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            //本帧 RT 管线会跑:实体叠加由 EndCaptureDraw 在历史位置负责,这里不能再画一遍
            if (RTPipelineAvailable) {
                return;
            }
            //玩家关掉绚丽特效时整条管线本就不画,兜底也不画(既有设计)
            if (!Config.Instance.EnablePixelEffect) {
                return;
            }
            //这两种情况下原本也不会有世界画面可叠,别把实体糊到全屏地图上
            if (Main.gameMenu || Main.mapFullscreen) {
                return;
            }

            spriteBatch.End();

            CEEntityOverlay.DrawScreenOverlayDirect();
            CEEntityOverlay.DrawLateOverlay();
            CECinematicScreen.DrawBlackMask();

            //还原本时机应有的批次状态:原版在 DrawInfernoRings 之前持有 Deferred/AlphaBlend/Main.Transform,
            //而本阶段实际排在 CEDrawHooks.DrawInfernoRingsHook 之后(InnoVault 先注册,故在其 orig 链内),
            //那边的 DrawAcropolisMechs 收尾时重开的也正是同一套
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState
                , DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
    }
}
