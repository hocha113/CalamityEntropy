using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 虚空驱逐舰全屏滤镜的客户端驱动(键 CalamityEntropy:VoidDestroyer,着色器 VDScreenFx.fxc)。
    /// 四条通道按帧「租约上报」:引力透镜(奇点)、空间裂隙位移(裂隙斩,最多 3 段)、暗角(护盾/主炮蓄力)、
    /// 冲击帧(黑白对比,整场一次)。状态与弹幕在 AI 里 Report*,本类在 PostUpdateEverything 把上报值
    /// 平滑进当前值并驱动 Filters.Scene 的激活/停用;没人上报的通道自然衰减到 0。
    /// 纯表现:服务端不会调用到这里的任何绘制路径,Report* 在 dedServ 上直接返回
    /// </summary>
    public static class VDScreenFx
    {
        public const string FilterKey = "CalamityEntropy:VoidDestroyer";
        public const int MaxRifts = 3;

        //上报槽(本帧)
        private static Vector2 pendLensCenter;
        private static float pendLensStrength;
        private static float pendLensRadius;
        private static readonly Vector4[] pendRift = new Vector4[MaxRifts];
        private static readonly float[] pendRiftOpen = new float[MaxRifts];
        private static int pendRiftCount;
        private static float pendVignette;
        private static float pendImpact;

        //当前值(平滑后,着色器读)
        private static Vector2 lensCenter;
        private static float lensStrength;
        private static float lensRadius;
        private static readonly Vector4[] rift = new Vector4[MaxRifts];
        private static readonly float[] riftOpen = new float[MaxRifts];
        private static float vignette;
        private static float impact;
        private static bool filterOn;

        public static bool Enabled => !Main.dedServ && Config.Instance.EnablePixelEffect;

        /// <summary>任一通道仍有可见量</summary>
        public static bool Active => lensStrength > 0.002f || vignette > 0.005f || impact > 0.005f || AnyRiftOpen();

        /// <summary>引力透镜:世界坐标中心、强度(0.05~0.12 可读)、半径(像素)</summary>
        public static void ReportLens(Vector2 worldCenter, float strength, float radius) {
            if (Main.dedServ || strength <= pendLensStrength) {
                return;
            }
            pendLensCenter = worldCenter;
            pendLensStrength = strength;
            pendLensRadius = radius;
        }

        /// <summary>空间裂隙:世界坐标线段 + 开口程度 0..1;每帧最多三段,多的忽略</summary>
        public static void ReportRift(Vector2 a, Vector2 b, float open) {
            if (Main.dedServ || pendRiftCount >= MaxRifts || open <= 0.001f) {
                return;
            }
            pendRift[pendRiftCount] = new Vector4(a.X, a.Y, b.X, b.Y);
            pendRiftOpen[pendRiftCount] = open;
            pendRiftCount++;
        }

        /// <summary>暗角压场 0..1</summary>
        public static void ReportVignette(float amount) {
            if (Main.dedServ) {
                return;
            }
            pendVignette = Math.Max(pendVignette, amount);
        }

        /// <summary>冲击帧 0..1(黑白高对比),调用方自己保证整场只用一次</summary>
        public static void ReportImpact(float amount) {
            if (Main.dedServ) {
                return;
            }
            pendImpact = Math.Max(pendImpact, amount);
        }

        /// <summary>点燃一次冲击帧:满值保持 frames 帧后再衰减(发起方可能当帧就消失,所以由驱动持有)</summary>
        public static void FireImpact(int frames) {
            if (Main.dedServ) {
                return;
            }
            impactHold = Math.Max(impactHold, frames);
        }

        private static int impactHold;

        /// <summary>每帧(PostUpdateEverything):上报值 → 当前值,清上报槽,驱动滤镜开关</summary>
        internal static void Update() {
            if (Main.dedServ) {
                return;
            }
            //透镜:有上报就跟过去,没上报衰减
            if (pendLensStrength > 0f) {
                lensCenter = pendLensCenter;
                lensRadius = pendLensRadius;
                lensStrength = MathHelper.Lerp(lensStrength, pendLensStrength, 0.3f);
            }
            else {
                lensStrength *= 0.85f;
            }
            //裂隙:直接覆盖(线段几何不能插值,开口量可)
            for (int i = 0; i < MaxRifts; i++) {
                if (i < pendRiftCount) {
                    rift[i] = pendRift[i];
                    riftOpen[i] = pendRiftOpen[i];
                }
                else {
                    riftOpen[i] *= 0.7f;
                }
            }
            vignette = pendVignette > vignette ? MathHelper.Lerp(vignette, pendVignette, 0.12f) : vignette * 0.94f;
            if (impactHold > 0) {
                impactHold--;
                impact = 1f;
            }
            else {
                impact = pendImpact > 0f ? pendImpact : impact * 0.6f;
            }

            pendLensStrength = 0f;
            pendRiftCount = 0;
            pendVignette = 0f;
            pendImpact = 0f;

            Filter filter = Filters.Scene[FilterKey];
            if (filter == null) {
                return;
            }
            //原版滤镜激活后全局不透明度以 1/秒爬升:12 帧的冲击帧、20 帧的裂隙等不起这一秒,
            //所以驱逐舰在场就常开(通道全零时着色器原样透传),离场且通道衰减归零后再关
            bool bossAlive = NPC.AnyNPCs(ModContent.NPCType<VoidDestroyer>());
            bool want = Enabled && (bossAlive || Active);
            if (want && !filterOn) {
                Filters.Scene.Activate(FilterKey, Main.LocalPlayer.Center);
                filterOn = true;
            }
            else if (!want && filterOn) {
                Filters.Scene.Deactivate(FilterKey);
                filterOn = false;
            }
        }

        internal static void Reset() {
            lensStrength = 0f;
            vignette = 0f;
            impact = 0f;
            impactHold = 0;
            for (int i = 0; i < MaxRifts; i++) {
                riftOpen[i] = 0f;
            }
            pendLensStrength = 0f;
            pendRiftCount = 0;
            pendVignette = 0f;
            pendImpact = 0f;
            if (filterOn && !Main.dedServ && Filters.Scene[FilterKey] != null) {
                Filters.Scene.Deactivate(FilterKey);
            }
            filterOn = false;
        }

        private static bool AnyRiftOpen() {
            for (int i = 0; i < MaxRifts; i++) {
                if (riftOpen[i] > 0.01f) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>世界坐标 → 滤镜 UV(经 GameViewMatrix,缩放与反重力翻转一并折进)</summary>
        private static Vector2 WorldToUv(Vector2 world) {
            Vector2 screenPx = Vector2.Transform(world - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
            return screenPx / new Vector2(Main.screenWidth, Main.screenHeight);
        }

        /// <summary>把当前值喂给着色器(由 <see cref="VDScreenShaderData.Apply"/> 每帧调用)</summary>
        internal static void Feed(Effect shader) {
            if (shader == null) {
                return;
            }
            float zoom = Main.GameViewMatrix.Zoom.X;
            Vector2 lensUv = WorldToUv(lensCenter);
            shader.Parameters["uLensCenter"]?.SetValue(lensUv);
            shader.Parameters["uLensStrength"]?.SetValue(lensStrength);
            shader.Parameters["uLensRadius"]?.SetValue(lensRadius * zoom / Main.screenHeight);

            Vector4[] riftUv = new Vector4[MaxRifts];
            for (int i = 0; i < MaxRifts; i++) {
                Vector2 a = WorldToUv(new Vector2(rift[i].X, rift[i].Y));
                Vector2 b = WorldToUv(new Vector2(rift[i].Z, rift[i].W));
                riftUv[i] = new Vector4(a.X, a.Y, b.X, b.Y);
            }
            shader.Parameters["uRift"]?.SetValue(riftUv);
            shader.Parameters["uRiftOpen"]?.SetValue(new Vector4(riftOpen[0], riftOpen[1], riftOpen[2], 0f));
            shader.Parameters["uVignette"]?.SetValue(vignette);
            shader.Parameters["uImpact"]?.SetValue(impact);
            shader.Parameters["uAspect"]?.SetValue(Main.screenWidth / (float)Main.screenHeight);
        }
    }

    /// <summary>
    /// 驱逐舰滤镜数据:强度门 = EnablePixelEffect(关则 uOpacity 归零、IsVisible 为假,滤镜整体被跳过),
    /// 参数每帧从 <see cref="VDScreenFx"/> 取。激活/停用由 VDScreenFx.Update 负责,本类不自灭
    /// </summary>
    public class VDScreenShaderData : ScreenShaderData
    {
        public VDScreenShaderData(Asset<Effect> shader, string passName) : base(shader, passName) {
        }

        public override void Apply() {
            UseOpacity(VDScreenFx.Enabled ? 1f : 0f);
            VDScreenFx.Feed(Shader);
            base.Apply();
        }
    }

    /// <summary>滤镜驱动的帧钩子:NPC 与弹幕都上报完再结算;换世界清零</summary>
    public class VDScreenFxSystem : ModSystem
    {
        public override void PostUpdateEverything() {
            VDScreenFx.Update();
        }

        public override void ClearWorld() {
            VDScreenFx.Reset();
        }

        public override void OnWorldUnload() {
            VDScreenFx.Reset();
        }
    }
}
