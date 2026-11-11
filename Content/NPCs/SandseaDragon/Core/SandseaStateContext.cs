using CalamityEntropy.Core.AI;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.Core
{
    /// <summary>状态声明意图,宿主统一落地</summary>
    public enum SandseaMoveMode
    {
        /// <summary>未声明:沙中减速,空中受重力</summary>
        Hold,
        /// <summary>状态自己写速度</summary>
        Direct,
        /// <summary>与目标保持相对静止</summary>
        HoldRelative,
    }

    /// <summary>事实字段要过线,声明每帧清回默认,漏声明就回到无害值</summary>
    public class SandseaStateContext : CEBossStateContext
    {
        public SandseaDragonHead Owner { get; set; }

        #region 事实:过线
        /// <summary>连接段开头选定的下一招,-1 为无</summary>
        public int PendingState { get; set; } = -1;
        /// <summary>本招从玩家的哪一侧出手,±1</summary>
        public int SideDir { get; set; } = 1;
        /// <summary>出手前锁死的点:跳跃的落点、盘旋的圆心</summary>
        public Vector2 LockedPoint { get; set; }
        /// <summary>出手前锁死的初速:跳跃与破沙的弹道</summary>
        public Vector2 LockedVelocity { get; set; }
        /// <summary>出手前锁死的角度:冲刺与俯冲的航向、盘旋的起始角</summary>
        public float LockedAngle { get; set; }
        /// <summary>竞技场中线的世界 X</summary>
        public float ArenaCenterX { get; set; }
        /// <summary>竞技场沙面的世界 Y</summary>
        public float ArenaFloorY { get; set; }
        /// <summary>边界旋风到中线的距离,三阶段逐帧收窄</summary>
        public float ArenaHalfWidth { get; set; } = SandseaDirector.ArenaHalfWidth;
        /// <summary>死亡演出已开始,CheckDead 拦住真死</summary>
        public bool Dying { get; set; }
        public bool DeathFinished { get; set; }
        #endregion

        #region 事实:只权威端用
        public int LeaveDesertTimer { get; set; }
        public int FarTimer { get; set; }
        #endregion

        #region 声明:运动
        public SandseaMoveMode Mode { get; set; }
        public Vector2 HoldOffset { get; set; }
        public float HoldStiffness { get; set; } = 0.12f;
        public float HoldLerp { get; set; } = 0.3f;
        public float HoldMaxSpeed { get; set; } = 26f;
        /// <summary>身体轨迹所在参考系的速度,悬停跟随玩家时让整条身体一起平移</summary>
        public Vector2 FrameVelocity { get; set; }
        /// <summary>本帧叠在自身速度上的牵连速度:在随玩家平移的参考系里操舵时用,下一帧操舵前要先扣掉</summary>
        public Vector2 Carry { get; set; }
        /// <summary>上一帧的牵连速度</summary>
        public Vector2 CarryPrev { get; private set; }
        /// <summary>本帧整条身体只是随头平移(自己写速度的跟随),速度方向与朝向无关</summary>
        public bool RigidFollow { get; set; }
        /// <summary>上一帧在悬停或平移跟随:这时航向要取头的朝向,不能取速度方向,否则头会倒着轧过自己的脖子</summary>
        public bool Held { get; private set; }
        #endregion

        #region 声明:判定
        /// <summary>接触伤害开关,演出态关掉</summary>
        public bool ContactActive { get; set; } = true;
        public bool Invulnerable { get; set; }
        /// <summary>从尾部数起已经炸掉的节数,死亡演出用</summary>
        public int SegmentsGone { get; set; }
        /// <summary>随冲沙鲨是否还贴在队形位上,松开后它们沿锁定航向自己飞</summary>
        public bool EscortHold { get; set; }
        #endregion

        #region 声明:表现(纯本地,不过线)
        /// <summary>下颌张开程度 0..1</summary>
        public float JawOpen { get; set; }
        /// <summary>摆尾幅度系数,冲刺时绷直</summary>
        public float Effort { get; set; } = 1f;
        /// <summary>弓身 -1..1,正值朝背侧弯</summary>
        public float Curl { get; set; }
        /// <summary>叠在阶段常态上的背鳍辉光 0..1</summary>
        public float GlowBoost { get; set; }
        /// <summary>眼部蓝光脉冲 0..1,自衰减</summary>
        public float EyeFlash { get; set; }
        /// <summary>冲刺前半身的黄色屏障 0..1</summary>
        public float Barrier { get; set; }
        /// <summary>透明度声明,NaN 时宿主拉回 1</summary>
        public float AlphaDeclared { get; set; } = float.NaN;
        /// <summary>全身抖动 0..1,只是绘制偏移,自衰减</summary>
        public float ShakeStrength { get; set; }
        /// <summary>白色粒子喷涌强度 0..1</summary>
        public float WhiteBurst { get; set; }
        /// <summary>直线航道预告,LaneStrength 为 0 不画</summary>
        public Vector2 LaneStart { get; set; }
        public Vector2 LaneDir { get; set; }
        public float LaneLength { get; set; }
        public float LaneHalfWidth { get; set; }
        public float LaneStrength { get; set; }
        /// <summary>抛物线弹道预告,ArcStrength 为 0 不画</summary>
        public Vector2 ArcStart { get; set; }
        public Vector2 ArcVelocity { get; set; }
        public float ArcGravity { get; set; }
        public float ArcStrength { get; set; }
        /// <summary>破沙预兆的沙面喷涌点,OmenStrength 为 0 不喷</summary>
        public Vector2 OmenPos { get; set; }
        public float OmenStrength { get; set; }
        #endregion

        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
            //先记下上一帧是不是悬停,再清声明
            Held = Mode == SandseaMoveMode.HoldRelative || RigidFollow;
            RigidFollow = false;
            CarryPrev = Carry;
            Carry = Vector2.Zero;
            Mode = SandseaMoveMode.Hold;
            HoldStiffness = 0.12f;
            HoldLerp = 0.3f;
            HoldMaxSpeed = 26f;
            FrameVelocity = Vector2.Zero;

            ContactActive = true;
            Invulnerable = false;
            SegmentsGone = 0;
            EscortHold = false;

            JawOpen = 0f;
            Effort = 1f;
            Curl = 0f;
            GlowBoost = 0f;
            Barrier = 0f;
            AlphaDeclared = float.NaN;
            WhiteBurst = 0f;
            LaneStrength = 0f;
            ArcStrength = 0f;
            OmenStrength = 0f;

            EyeFlash *= 0.9f;
            if (EyeFlash < 0.02f) {
                EyeFlash = 0f;
            }
            ShakeStrength *= 0.84f;
            if (ShakeStrength < 0.02f) {
                ShakeStrength = 0f;
            }
        }
    }
}
