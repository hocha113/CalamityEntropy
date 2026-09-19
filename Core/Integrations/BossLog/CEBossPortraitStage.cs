using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using Terraria;

namespace CalamityEntropy.Core.Integrations.BossLog
{
    /// <summary>
    /// customPortrait 一帧的舞台参数。演员在「虚拟场景坐标」(原点=画布中心)里模拟与绘制,
    /// <see cref="WorldMatrix"/> 负责把场景坐标映射进图鉴页画布(含 UIScale 与缩放)
    /// </summary>
    internal readonly struct CEPortraitFrame
    {
        /// <summary>画布(UI 空间,已避开 BossChecklist 叠画的标题区)</summary>
        public readonly Rectangle Canvas;
        /// <summary>场景坐标 → 屏幕的批次矩阵</summary>
        public readonly Matrix WorldMatrix;
        /// <summary>裁剪光栅态(演员中途重启批次时必须沿用,保持画布裁剪)</summary>
        public readonly RasterizerState Scissor;
        /// <summary>进度隐藏蒙版色(正常 White,隐藏 Black)</summary>
        public readonly Color Mask;
        /// <summary>剪影模式(蒙版近黑):体色归黑、加色辉光层跳过</summary>
        public readonly bool Masked;
        /// <summary>画布在场景坐标下的可视半宽/半高(背景铺满用)</summary>
        public readonly Vector2 SceneHalf;

        public CEPortraitFrame(Rectangle canvas, Matrix worldMatrix, RasterizerState scissor,
            Color mask, bool masked, Vector2 sceneHalf) {
            Canvas = canvas;
            WorldMatrix = worldMatrix;
            Scissor = scissor;
            Mask = mask;
            Masked = masked;
            SceneHalf = sceneHalf;
        }

        /// <summary>体色蒙版乘算(剪影模式贴图形状保留、颜色归黑)</summary>
        public Color Tint(Color color) => Masked ? color.MultiplyRGB(Mask) : color;

        /// <summary>不透明压暗(剪影模式的场景层保亮度层次、不透明度不动;正常模式原样返回)</summary>
        public Color Dim(Color color, float maskedMul = 0.42f) {
            if (!Masked) {
                return color;
            }
            return new Color((int)(color.R * maskedMul), (int)(color.G * maskedMul), (int)(color.B * maskedMul), color.A);
        }
    }

    /// <summary>
    /// 图鉴沙盒演员:一个 Boss 的实时演出(headless 模拟 + 绘制)。
    /// 只在图鉴选中页可见时被驱动,纯客户端表现,不碰任何世界/NPC 状态
    /// </summary>
    internal abstract class CEBossPortraitActor
    {
        /// <summary>场景半尺寸(场景坐标;舞台据此定缩放)</summary>
        public abstract Vector2 SceneHalfSize { get; }

        /// <summary>整本书接管时的域主题(色板 + 书后氛围 + 书缘饰件)</summary>
        public abstract CEBossLogTheme Theme { get; }

        /// <summary>场景时钟(秒,重置归零)</summary>
        protected float Time { get; private set; }

        /// <summary>上次绘制的高精度时戳(Stopwatch tick,舞台步进与离页判定用)</summary>
        internal long LastStamp;

        /// <summary>待偿步进债(秒,舞台固定步进积累器)</summary>
        internal float StepDebt;

        internal void Step(float dt) {
            Time += dt;
            Update(dt);
        }

        internal void ResetScene() {
            Time = 0f;
            Reset();
        }

        /// <summary>推进一帧(dt 已限幅,秒)</summary>
        protected abstract void Update(float dt);

        /// <summary>场景重置(首次进入或翻页离开太久后重开演出)</summary>
        protected abstract void Reset();

        /// <summary>绘制:批次已按舞台矩阵开启,直接以场景坐标绘制</summary>
        public abstract void Draw(SpriteBatch sb, in CEPortraitFrame frame);
    }

    /// <summary>
    /// 图鉴头像沙盒舞台:画布划定、裁剪、批次接管与恢复、进度隐藏蒙版、墙钟步进与过期重置。
    /// 两条入口:<see cref="Draw"/> 是 BossChecklist <c>customPortrait</c> 回调的回退路径
    /// (自己在页内让出标题区);<see cref="DrawScene"/> 由整本书接管钩子 <see cref="CEBossLogHook"/>
    /// 调用,画布由骨架按主题标题带算好后直接移交。
    /// 暂停时动画照常呼吸(墙钟驱动);只在图鉴页被绘制时产生开销
    /// </summary>
    internal static class CEBossPortraitStage
    {
        /// <summary>顶部预留(回退路径):BossChecklist 会在页面上叠画 Boss 名、来源模组与右上头图标</summary>
        private const int TopReserve = 52;
        private const int EdgeInset = 8;
        /// <summary>固定逻辑步长(与游戏 60fps 逻辑帧一致)</summary>
        private const float StepSeconds = 1f / 60f;
        /// <summary>单次绘制最多补几步:低帧率下动画减速而不快进跳变</summary>
        private const int MaxStepsPerDraw = 3;
        /// <summary>离页重开阈值(秒):离开该页再回来重新开演;中途卡顿尖峰不算</summary>
        private const float StaleSeconds = 2.5f;

        private static readonly RasterizerState scissorState = new() {
            CullMode = CullMode.None,
            ScissorTestEnable = true,
        };

        /// <summary>customPortrait 回调入口(回退路径):整页矩形进来,自己让出顶部标题区</summary>
        public static void Draw(SpriteBatch sb, Rectangle pageRect, Color mask, CEBossPortraitActor actor) {
            Rectangle canvas = new(pageRect.X + EdgeInset, pageRect.Y + TopReserve,
                pageRect.Width - EdgeInset * 2, pageRect.Height - TopReserve - EdgeInset);
            DrawScene(sb, canvas, mask, actor);
        }

        /// <summary>按给定画布画一帧场景(步进、缩放、裁剪、批次接管与恢复全在这里)</summary>
        public static void DrawScene(SpriteBatch sb, Rectangle canvas, Color mask, CEBossPortraitActor actor) {
            if (Main.dedServ || actor == null || sb == null) {
                return;
            }
            if (canvas.Width < 60 || canvas.Height < 60) {
                return;
            }

            //固定步进积累器:动画恒按 60fps 逻辑步长推进,绘制率/时钟粒度/同帧多次回调都不影响速度。
            //不要拿 TickCount64 毫秒差直接当 dt:15.6ms 计时粒度在高刷新率下频繁读出 0ms,
            //会被误判成断绘触发整场重置,表现为开场反复回退卡顿
            long now = Stopwatch.GetTimestamp();
            if (actor.LastStamp == 0) {
                actor.ResetScene();
                actor.StepDebt = StepSeconds;
            }
            else {
                double gapSec = (now - actor.LastStamp) / (double)Stopwatch.Frequency;
                if (gapSec > StaleSeconds) {
                    //真离页重开演出;中途卡顿只按上限补步,不重置
                    actor.ResetScene();
                    actor.StepDebt = StepSeconds;
                }
                else if (gapSec > 0) {
                    actor.StepDebt = MathF.Min(actor.StepDebt + (float)gapSec,
                        StepSeconds * (MaxStepsPerDraw + 0.5f));
                }
            }
            actor.LastStamp = now;

            int steps = 0;
            while (actor.StepDebt >= StepSeconds && steps < MaxStepsPerDraw) {
                actor.StepDebt -= StepSeconds;
                actor.Step(StepSeconds);
                steps++;
            }

            Vector2 half = actor.SceneHalfSize;
            float zoom = MathF.Min(canvas.Width / (half.X * 2f), canvas.Height / (half.Y * 2f));
            if (zoom <= 0f) {
                return;
            }
            Vector2 center = canvas.Center.ToVector2();
            Matrix worldMatrix = Matrix.CreateScale(zoom, zoom, 1f)
                * Matrix.CreateTranslation(center.X, center.Y, 0f)
                * Main.UIScaleMatrix;

            bool masked = mask.R < 40 && mask.G < 40 && mask.B < 40;
            CEPortraitFrame frame = new(canvas, worldMatrix, scissorState, mask, masked,
                new Vector2(canvas.Width * 0.5f / zoom, canvas.Height * 0.5f / zoom));

            //接管批次:画布裁剪 + 场景矩阵;结束后恢复标准 UI 批次
            GraphicsDevice gd = sb.GraphicsDevice;
            Rectangle prevScissor = gd.ScissorRectangle;
            sb.End();
            gd.ScissorRectangle = UiToScreen(canvas, gd);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, scissorState, null, worldMatrix);

            try {
                actor.Draw(sb, in frame);
            } finally {
                sb.End();
                gd.ScissorRectangle = prevScissor;
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            }
        }

        /// <summary>场景标准批次(演员从加色/着色器批切回时用)</summary>
        public static void BeginAlpha(SpriteBatch sb, in CEPortraitFrame frame) {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, frame.Scissor, null, frame.WorldMatrix);
        }

        /// <summary>加色批(辉光层;调用方负责先 End)</summary>
        public static void BeginAdditive(SpriteBatch sb, in CEPortraitFrame frame) {
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, frame.Scissor, null, frame.WorldMatrix);
        }

        /// <summary>着色器批(Immediate:允许逐 Draw 改参;调用方负责先 End)</summary>
        public static void BeginShader(SpriteBatch sb, in CEPortraitFrame frame, Effect effect) {
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, frame.Scissor, effect, frame.WorldMatrix);
        }

        /// <summary>UI 矩形 → 屏幕像素裁剪矩形(按 UIScale 变换并钳进视口)</summary>
        private static Rectangle UiToScreen(Rectangle ui, GraphicsDevice gd) {
            Vector2 tl = Vector2.Transform(new Vector2(ui.X, ui.Y), Main.UIScaleMatrix);
            Vector2 br = Vector2.Transform(new Vector2(ui.Right, ui.Bottom), Main.UIScaleMatrix);
            Rectangle rect = new((int)tl.X, (int)tl.Y,
                (int)MathF.Ceiling(br.X - tl.X), (int)MathF.Ceiling(br.Y - tl.Y));
            return Rectangle.Intersect(rect, gd.Viewport.Bounds);
        }
    }
}
