using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Capture;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>
    /// 渲染帧戳:每个绘制帧自增一次,供 <see cref="CESkyBase"/> 的切片门控去重。
    /// SkyManager.DrawToDepth 每帧按视差层回调十余次(次数随生物群系变化),
    /// 镜头贴近地狱时 DrawUnderworldBackground 还会重置深度追踪器再来一轮,
    /// 单靠切片条件一帧内可能双触发,须以戳记兜底。
    /// ModifyTransformMatrix 在 Main.DoDraw 里每渲染帧恰好执行一次,且早于全部背景绘制。
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class CESkyFrameStamp : ModSystem
    {
        public static int Current { get; private set; }

        public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform) => Current++;
    }

    /// <summary>
    /// 天空绘制的分辨率无关工具集。背景绘制窗口内(Main.DoDraw → DrawBG)原版篡改了三件事:
    /// Main.screenWidth/Height 被 BackgroundViewMatrix.Zoom(= ForcedMinimumZoom,高分屏大于 1)预除、
    /// Main.screenPosition 被加上 BackgroundViewMatrix.Translation、批次矩阵是带平移补偿的背景矩阵。
    /// 天空代码要么留在调用方空间(预除尺寸 + 背景矩阵),要么走原始像素空间(Viewport 尺寸 + 无矩阵),
    /// 两种空间不可混用;历史 Bug(4K 铺不满、缩放漂移)全部源于混用。
    /// </summary>
    public static class CESkyDrawing
    {
        /// <summary>还原 Main.DoDraw 传给 DrawBG 的背景矩阵(含重力翻转与背景缩放平移补偿)。</summary>
        public static Matrix BackgroundMatrix() {
            Matrix m = Main.BackgroundViewMatrix.TransformationMatrix;
            m.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation
                * new Vector3(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f, 1f);
            return m;
        }

        /// <summary>背景窗口内的真实相机位置(原版在进 DrawBG 前给 screenPosition 加了背景缩放平移)。</summary>
        public static Vector2 RealScreenPosition => Main.screenPosition - Main.BackgroundViewMatrix.Translation;

        /// <summary>调用方空间的全屏矩形:预除后的 screenWidth/Height 配合调用方批次矩阵恰好铺满。</summary>
        public static Rectangle CallerFullscreen => new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);

        /// <summary>原始像素空间的全屏矩形(配无矩阵批次的全屏 shader pass 用)。</summary>
        public static Rectangle ViewportFullscreen {
            get {
                Viewport vp = Main.instance.GraphicsDevice.Viewport;
                return new Rectangle(0, 0, vp.Width, vp.Height);
            }
        }

        /// <summary>End 后按指定混合/采样在调用方(背景矩阵)空间重开批次。</summary>
        public static void BeginCallerSpace(SpriteBatch sb, BlendState blend, SamplerState sampler, SpriteSortMode sort = SpriteSortMode.Deferred) {
            sb.End();
            sb.Begin(sort, blend, sampler, DepthStencilState.None, Main.Rasterizer, null, BackgroundMatrix());
        }

        /// <summary>End 后在原始像素空间(无矩阵)重开批次,给全屏 shader pass 用。</summary>
        public static void BeginRawScreen(SpriteBatch sb, BlendState blend, SamplerState sampler, SpriteSortMode sort = SpriteSortMode.Immediate) {
            sb.End();
            sb.Begin(sort, blend, sampler, DepthStencilState.None, RasterizerState.CullNone, null);
        }

        /// <summary>不 End、直接按调用方参数开批次(配合已手动 End 过的场合,如图元渲染之后)。</summary>
        public static void OpenCallerBatch(SpriteBatch sb) {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, BackgroundMatrix());
        }

        /// <summary>按 Main.DoDraw 开 DrawBG 批次的参数还原调用方批次;自开批次画完后必须调用。</summary>
        public static void RestoreCallerBatch(SpriteBatch sb) {
            sb.End();
            OpenCallerBatch(sb);
        }
    }

    /// <summary>
    /// 天空基座:统一淡入淡出生命周期、深度切片门控、帧戳去重与相机捕捉守卫。
    /// 载荷写进 <see cref="DrawFar"/>(最远切片,画在一切视差背景层后面)或
    /// <see cref="DrawFront"/>(跨 0 切片,画在视差背景层前面、游戏世界后面),
    /// 两者每渲染帧各至多执行一次;自开批次必须以 <see cref="CESkyDrawing.RestoreCallerBatch"/> 收尾。
    /// 状态推进(计数器、粒子生灭、音效)一律放 <see cref="UpdatePayload"/>,Draw 只读。
    /// </summary>
    public abstract class CESkyBase : CustomSky
    {
        protected bool skyActive;
        protected float opacity;
        private int lastFarStamp = -1;
        private int lastFrontStamp = -1;

        /// <summary>淡入步长(每 tick)。</summary>
        protected virtual float FadeInStep => 0.02f;

        /// <summary>淡出步长(每 tick),默认与淡入一致。</summary>
        protected virtual float FadeOutStep => FadeInStep;

        /// <summary>
        /// 驱动源是否仍要求天空在场。Update 每 tick 双向重推导 skyActive,
        /// 免疫 ManageSpecialBiomeVisuals 在淡出期(IsActive 仍为 true)不再回调 Activate 的短路。
        /// </summary>
        protected abstract bool KeepActive();

        public override void Activate(Vector2 position, params object[] args) => skyActive = true;

        public override void Deactivate(params object[] args) => skyActive = false;

        public override void Reset() {
            skyActive = false;
            opacity = 0f;
            OnReset();
        }

        /// <summary>Reset 时清理载荷状态(粒子列表等)。</summary>
        protected virtual void OnReset() { }

        //淡出尾巴必须算 Active:SkyManager 只更新/绘制 IsActive 的天空,提前返回 false 会冻住渐隐
        public override bool IsActive() => skyActive || opacity > 0f;

        public sealed override void Update(GameTime gameTime) {
            skyActive = !Main.gameMenu && KeepActive();

            if (skyActive && opacity < 1f)
                opacity = Math.Min(1f, opacity + FadeInStep);
            else if (!skyActive && opacity > 0f)
                opacity = Math.Max(0f, opacity - FadeOutStep);

            Opacity = opacity;
            UpdatePayload(gameTime);
        }

        /// <summary>每 tick 的载荷状态推进(在 opacity 步进之后调用)。</summary>
        protected virtual void UpdatePayload(GameTime gameTime) { }

        public sealed override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) {
            if (opacity <= 0.004f)
                return;
            //捕捉/全屏地图路径的批次是裸 Begin(),背景矩阵还原公式在那里不成立,直接不画
            if (Main.mapFullscreen || CaptureManager.Instance.IsCapturing)
                return;

            //最远切片:ResetDepthTracker 后的首个 DrawToDepth,maxDepth 为 float.MaxValue
            if (maxDepth >= float.MaxValue && minDepth < float.MaxValue && lastFarStamp != CESkyFrameStamp.Current) {
                lastFarStamp = CESkyFrameStamp.Current;
                DrawFar(spriteBatch);
            }

            //跨 0 切片:DrawRemainingDepth 的 (float.MinValue, ≥0) 调用
            if (minDepth < 0f && maxDepth >= 0f && maxDepth < float.MaxValue && lastFrontStamp != CESkyFrameStamp.Current) {
                lastFrontStamp = CESkyFrameStamp.Current;
                DrawFront(spriteBatch);
            }
        }

        /// <summary>最远切片载荷:画在一切视差背景层后面。</summary>
        protected virtual void DrawFar(SpriteBatch spriteBatch) { }

        /// <summary>跨 0 切片载荷:画在视差背景层前面、游戏世界后面。</summary>
        protected virtual void DrawFront(SpriteBatch spriteBatch) { }
    }
}
