using CalamityEntropy.Core.AI;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core
{
    /// <summary>状态声明意图,宿主统一落地</summary>
    public enum SageMoveMode
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

    /// <summary>本体头朝哪边</summary>
    public enum SageFaceMode
    {
        /// <summary>朝玩家</summary>
        Target,
        /// <summary>朝运动方向,速度太小时保持原样</summary>
        Velocity,
        /// <summary>朝 FaceAngle</summary>
        Angle,
    }

    /// <summary>事实字段要过线,声明每帧清回默认,漏声明就回到无害值</summary>
    public class SageStateContext : CEBossStateContext
    {
        public StarmoonSage Owner { get; set; }

        #region 事实:过线
        /// <summary>连接段开头选定的下一招,-1 为无</summary>
        public int PendingState { get; set; } = -1;
        /// <summary>侧向 ±1,每手翻转</summary>
        public int SideDir { get; set; } = 1;
        /// <summary>星遁落点</summary>
        public Vector2 AnchorPos { get; set; }
        /// <summary>出手前锁死的角度,冲刺航向和扇面中线都存这里</summary>
        public float LockedAngle { get; set; }
        /// <summary>整招的基准方位,起手时定一次,之后各拍按它推算</summary>
        public float BaseAngle { get; set; }
        /// <summary>大于 0 正在星遁</summary>
        public int BlinkTimer { get; set; }
        public int NoContactTimer { get; set; }
        /// <summary>死亡演出已开始,CheckDead 拦住真死</summary>
        public bool Dying { get; set; }
        public bool DeathFinished { get; set; }
        #endregion

        #region 声明:运动
        public SageMoveMode Mode { get; set; }
        public Vector2 MoveTarget { get; set; }
        public float MoveSpeed { get; set; }
        public float Accel { get; set; } = 0.1f;
        public float SlowRadius { get; set; } = 120f;
        public Vector2 HoldOffset { get; set; }
        public float Stiffness { get; set; } = 0.12f;
        public SageFaceMode Face { get; set; }
        public float FaceAngle { get; set; }
        #endregion

        #region 声明:判定
        public bool ContactWindow { get; set; }
        /// <summary>演出态声明,本帧不可受伤</summary>
        public bool Invulnerable { get; set; }
        #endregion

        #region 声明:表现(纯本地,不过线)
        /// <summary>翼姿:+1 大展,0 常态,-1 后掠收拢</summary>
        public float WingSpread { get; set; }
        /// <summary>月轮亮度目标 0..1,不声明回到阶段常态值</summary>
        public float CrestGlow { get; set; }
        /// <summary>月轮爆闪脉冲 0..1,自衰减</summary>
        public float CrestPulse { get; set; }
        /// <summary>全身星辉叠层 0..1</summary>
        public float BodyGlow { get; set; }
        /// <summary>嘴前的施法星印强度 0..1</summary>
        public float CastGlow { get; set; }
        /// <summary>透明度声明,NaN 时宿主拉回 1</summary>
        public float AlphaDeclared { get; set; } = float.NaN;
        /// <summary>全身抖动 0..1,只是绘制偏移,自衰减</summary>
        public float ShakeStrength { get; set; }
        /// <summary>冲刺航道预告,LaneStrength 为 0 不画</summary>
        public Vector2 LaneStart { get; set; }
        public Vector2 LaneDir { get; set; }
        public float LaneLength { get; set; }
        public float LaneStrength { get; set; }
        /// <summary>星流扇的航线预告:道数和强度,中线读 LockedAngle</summary>
        public int FanLanes { get; set; }
        public float FanStrength { get; set; }
        /// <summary>星环:总槽数和已经打出去的颗数,剩下的由宿主画在身周</summary>
        public int RingSlots { get; set; }
        public int RingFired { get; set; }
        /// <summary>0 号槽当前的方位角,状态按它生成星符,宿主按它画</summary>
        public float RingAngle { get; set; }
        public float RingGlow { get; set; }
        /// <summary>死亡演出从体内迸出的星芒强度 0..1</summary>
        public float DeathStars { get; set; }
        /// <summary>天幕存在强度 0..1,出场和退场由状态编排</summary>
        public float SkyIntensity { get; set; } = 1f;
        #endregion

        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
            Mode = SageMoveMode.Hold;
            MoveSpeed = 0f;
            Accel = 0.1f;
            SlowRadius = 120f;
            Stiffness = 0.12f;
            Face = SageFaceMode.Target;

            ContactWindow = false;
            Invulnerable = false;

            WingSpread = 0f;
            CrestGlow = SageDirector.CrestIdle(Phase);
            BodyGlow = 0f;
            CastGlow = 0f;
            AlphaDeclared = float.NaN;
            LaneStrength = 0f;
            FanLanes = 0;
            FanStrength = 0f;
            RingSlots = 0;
            RingFired = 0;
            RingGlow = 0f;
            DeathStars = 0f;
            SkyIntensity = 1f;

            CrestPulse *= 0.9f;
            if (CrestPulse < 0.02f) {
                CrestPulse = 0f;
            }
            ShakeStrength *= 0.84f;
            if (ShakeStrength < 0.02f) {
                ShakeStrength = 0f;
            }
        }
    }
}