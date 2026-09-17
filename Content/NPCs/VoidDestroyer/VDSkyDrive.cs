using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 「轨道封锁」天幕的客户端驱动(与 <see cref="VDScreenFx"/> 同一租约模式,参照 CruiserSkyDrive):
    /// 宿主每帧 <see cref="Report"/> 存在强度 / 阶段 / 本体位置 / 核心亮度,状态在拍点 <see cref="PushFlash"/>、
    /// 主炮蓄力 <see cref="ReportCharge"/>;<see cref="Update"/>(VDScreenFxSystem.PostUpdateEverything)把上报值平滑进当前值,
    /// 没人续租的通道自然衰减。天空件 VDSky 只读这里的当前值。
    /// 纯表现:各端本地观察自己的状态机副本,不走包;dedServ 上一切上报直接返回
    /// </summary>
    public static class VDSkyDrive
    {
        /// <summary>存在强度 0..1(平滑后),与 VDSky 的存在包络 opacity 相乘成可见强度</summary>
        public static float Intensity { get; private set; }
        /// <summary>阶段 1..3(平滑浮点,配色与网格半径插值用)</summary>
        public static float Phase { get; private set; } = 1f;
        /// <summary>星球侵蚀量 0..1(慢速逼近阶段档位)</summary>
        public static float Erosion { get; private set; }
        /// <summary>拍点闪光 0..1(自衰减)</summary>
        public static float Flash { get; private set; }
        /// <summary>冲击环当前半径(屏高单位),小于 0 为无环</summary>
        public static float FlashRing { get; private set; } = -1f;
        /// <summary>主炮蓄力 0..1</summary>
        public static float Charge { get; private set; }
        /// <summary>本体核心亮度 0..1(网格以本体为中心的亮化乘它)</summary>
        public static float BossGlow { get; private set; }
        /// <summary>本体世界坐标(天空件换算成屏幕 UV)</summary>
        public static Vector2 BossWorldPos { get; private set; }
        /// <summary>
        /// 天幕视差原点:开打第一帧的本体位置。视差偏移按「相机 − 原点」算而不是绝对世界坐标,
        /// 否则大世界里 13 万像素的横坐标会把星球推出屏幕、把星野 hash 的浮点精度吃光
        /// </summary>
        public static Vector2 OriginWorldPos { get; private set; }
        /// <summary>本帧有宿主续租</summary>
        public static bool LeaseAlive { get; private set; }

        /// <summary>任一通道仍有可见量(场景保活的判据)</summary>
        public static bool Active => Intensity > 0.004f;

        //上报槽(本帧)
        private static float pendIntensity;
        private static int pendPhase = 1;
        private static Vector2 pendBossPos;
        private static float pendGlow;
        private static float pendCharge;
        private static float pendFlash;
        private static bool pendLease;
        private static int ringAge = -1;

        /// <summary>宿主每帧续租:存在强度(出场/死亡/撤离由宿主按状态编排好斜坡)、阶段、本体位置、核心亮度</summary>
        public static void Report(float intensity, int phase, Vector2 bossWorldPos, float coreGlow) {
            if (Main.dedServ) {
                return;
            }
            pendIntensity = Math.Max(pendIntensity, MathHelper.Clamp(intensity, 0f, 1f));
            pendPhase = Math.Max(pendPhase, Math.Clamp(phase, 1, 3));
            pendBossPos = bossWorldPos;
            pendGlow = Math.Max(pendGlow, MathHelper.Clamp(coreGlow, 0f, 1f));
            pendLease = true;
        }

        /// <summary>主炮蓄力读数 0..1(蓄力拍每帧上报,扫射/过热上报较低的保持值)</summary>
        public static void ReportCharge(float charge) {
            if (Main.dedServ) {
                return;
            }
            pendCharge = Math.Max(pendCharge, MathHelper.Clamp(charge, 0f, 1f));
        }

        /// <summary>拍点闪光(转阶段 / 主炮出手 1,hub 起势轻脉冲);同帧取最大</summary>
        public static void PushFlash(float strength) {
            if (Main.dedServ || strength <= 0f) {
                return;
            }
            pendFlash = Math.Max(pendFlash, MathHelper.Clamp(strength, 0f, 1f));
        }

        /// <summary>每帧结算:上报值 → 当前值,清上报槽</summary>
        internal static void Update() {
            if (Main.dedServ) {
                return;
            }
            bool wasAlive = LeaseAlive;
            LeaseAlive = pendLease;

            //新一场开打:视差原点钉在本体出场位置
            if (!wasAlive && pendLease && Intensity < 0.05f) {
                OriginWorldPos = pendBossPos;
            }

            if (pendLease) {
                Intensity = MoveToward(Intensity, pendIntensity, VDDirector.SkyTrackPerTick);
                Phase = MathHelper.Lerp(Phase, pendPhase, VDDirector.SkyPhaseLerp);
                BossWorldPos = pendBossPos;
                BossGlow = MathHelper.Lerp(BossGlow, pendGlow, 0.3f);
            }
            else {
                Intensity = Math.Max(0f, Intensity - VDDirector.SkyFallPerTick);
                BossGlow *= 0.9f;
            }

            //侵蚀:新一场开打时直接落到本阶段档位(上一场留下的 0.7 不该在门开时倒着「愈合」),之后慢速逼近
            float erosionTarget = VDDirector.SkyErosion(pendPhase);
            if (!wasAlive && pendLease && Intensity < 0.05f) {
                Erosion = erosionTarget;
            }
            else if (pendLease) {
                Erosion = MoveToward(Erosion, erosionTarget, VDDirector.SkyErosionPerTick);
            }

            //闪光:同帧取最大后指数衰减;够强的拍点从本体处放一圈冲击环
            Flash = Math.Max(Flash * VDDirector.SkyFlashDecay, pendFlash);
            if (Flash < 0.003f) {
                Flash = 0f;
            }
            if (pendFlash >= VDDirector.SkyFlashRingThreshold) {
                ringAge = 0;
            }
            else if (ringAge >= 0) {
                ringAge++;
                if (ringAge > VDDirector.SkyFlashRingLife) {
                    ringAge = -1;
                }
            }
            FlashRing = ringAge < 0 ? -1f : ringAge * VDDirector.SkyFlashRingSpeed;

            Charge = pendCharge > 0f ? MathHelper.Lerp(Charge, pendCharge, 0.3f) : Charge * VDDirector.SkyChargeDecay;
            if (Charge < 0.003f) {
                Charge = 0f;
            }

            pendIntensity = 0f;
            pendPhase = 1;
            pendGlow = 0f;
            pendCharge = 0f;
            pendFlash = 0f;
            pendLease = false;
        }

        internal static void Reset() {
            Intensity = 0f;
            Phase = 1f;
            Erosion = 0f;
            Flash = 0f;
            FlashRing = -1f;
            Charge = 0f;
            BossGlow = 0f;
            BossWorldPos = Vector2.Zero;
            OriginWorldPos = Vector2.Zero;
            LeaseAlive = false;
            pendIntensity = 0f;
            pendPhase = 1;
            pendGlow = 0f;
            pendCharge = 0f;
            pendFlash = 0f;
            pendLease = false;
            ringAge = -1;
        }

        private static float MoveToward(float current, float target, float maxStep) {
            float diff = target - current;
            if (Math.Abs(diff) <= maxStep) {
                return target;
            }
            return current + Math.Sign(diff) * maxStep;
        }
    }
}
