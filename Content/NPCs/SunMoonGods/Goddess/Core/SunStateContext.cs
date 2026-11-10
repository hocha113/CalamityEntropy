using CalamityEntropy.Core.AI;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core
{
    /// <summary>状态声明意图,宿主统一落地</summary>
    public enum SunMoveMode
    {
        /// <summary>未声明,指数刹停</summary>
        Hold,
        /// <summary>朝目标点飞,带减速带</summary>
        HoverTo,
        /// <summary>与目标保持相对静止</summary>
        HoldRelative,
        /// <summary>状态自己写速度</summary>
        Direct,
    }

    /// <summary>事实字段要过线,声明每帧清回默认,漏声明就回到无害值</summary>
    public class SunStateContext : CEBossStateContext
    {
        public SunGoddess Owner { get; set; }

        #region 事实:过线
        /// <summary>连接段开头选定的下一招,-1 为无</summary>
        public int PendingState { get; set; } = -1;
        /// <summary>侧向 ±1,每手翻转</summary>
        public int SideDir { get; set; } = 1;
        /// <summary>闪现落点</summary>
        public Vector2 AnchorPos { get; set; }
        /// <summary>出手前锁死的点,冲刺起点和坠日落点都存这里</summary>
        public Vector2 LockedPoint { get; set; }
        /// <summary>出手前锁死的角度,冲刺航向和射线基准角都存这里</summary>
        public float LockedAngle { get; set; }
        /// <summary>大于 0 正在化光闪现</summary>
        public int BlinkTimer { get; set; }
        public int NoContactTimer { get; set; }
        /// <summary>死亡演出已开始,CheckDead 拦住真死</summary>
        public bool Dying { get; set; }
        public bool DeathFinished { get; set; }
        #endregion

        #region 声明:运动
        public SunMoveMode Mode { get; set; }
        public Vector2 MoveTarget { get; set; }
        public float MoveSpeed { get; set; }
        public float Accel { get; set; } = 0.1f;
        public float SlowRadius { get; set; } = 120f;
        public Vector2 HoldOffset { get; set; }
        public float Stiffness { get; set; } = 0.12f;
        /// <summary>倾斜覆盖,单位是弧度,NaN 跟横向速度走</summary>
        public float TiltOverride { get; set; } = float.NaN;
        #endregion

        #region 声明:判定
        public bool ContactWindow { get; set; }
        /// <summary>演出态声明,本帧不可受伤</summary>
        public bool Invulnerable { get; set; }
        #endregion

        #region 声明:表现(纯本地,不过线)
        /// <summary>手臂帧 0..6,右臂用同一张图镜像</summary>
        public int ArmLeft { get; set; }
        public int ArmRight { get; set; }
        /// <summary>日轮亮度目标 0..1,不声明回到阶段常态值</summary>
        public float HaloGlow { get; set; }
        /// <summary>日轮爆闪脉冲 0..1,自衰减</summary>
        public float HaloPulse { get; set; }
        /// <summary>翼姿:+1 收拢,0 常态,-1 高扬</summary>
        public float WingPose { get; set; }
        /// <summary>全身白热叠层 0..1</summary>
        public float BodyGlow { get; set; }
        /// <summary>透明度声明,NaN 时宿主拉回 1</summary>
        public float AlphaDeclared { get; set; } = float.NaN;
        /// <summary>全身抖动 0..1,只是绘制偏移,自衰减</summary>
        public float ShakeStrength { get; set; }
        /// <summary>左右手的施法光印强度 0..1</summary>
        public float CastLeft { get; set; }
        public float CastRight { get; set; }
        /// <summary>冲刺航道预告,LaneStrength 为 0 不画</summary>
        public Vector2 LaneStart { get; set; }
        public Vector2 LaneDir { get; set; }
        public float LaneLength { get; set; }
        public float LaneStrength { get; set; }
        /// <summary>死亡演出从体内迸出的光柱强度 0..1</summary>
        public float DeathRays { get; set; }
        /// <summary>天幕存在强度 0..1,出场和退场由状态编排</summary>
        public float SkyIntensity { get; set; } = 1f;
        #endregion

        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
            Mode = SunMoveMode.Hold;
            MoveSpeed = 0f;
            Accel = 0.1f;
            SlowRadius = 120f;
            Stiffness = 0.12f;
            TiltOverride = float.NaN;

            ContactWindow = false;
            Invulnerable = false;

            ArmLeft = 0;
            ArmRight = 0;
            HaloGlow = SunDirector.HaloIdle(Phase);
            WingPose = 0f;
            BodyGlow = 0f;
            AlphaDeclared = float.NaN;
            CastLeft = 0f;
            CastRight = 0f;
            LaneStrength = 0f;
            DeathRays = 0f;
            SkyIntensity = 1f;

            HaloPulse *= 0.9f;
            if (HaloPulse < 0.02f) {
                HaloPulse = 0f;
            }
            ShakeStrength *= 0.84f;
            if (ShakeStrength < 0.02f) {
                ShakeStrength = 0f;
            }
        }
    }
}