using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>运动声明模式:状态声明意图,宿主统一落地</summary>
    public enum VDMoveMode
    {
        /// <summary>未声明:指数刹停,绝不留残速漂移</summary>
        Hold,
        /// <summary>朝目标点平滑飞行(速度上限 + 减速带)</summary>
        HoverTo,
        /// <summary>与目标保持相对静止(前馈目标速度 + 偏差回位)</summary>
        HoldRelative,
        /// <summary>状态自管速度</summary>
        Direct,
    }

    /// <summary>
    /// 虚空驱逐舰状态上下文:每帧声明总线 + 跨帧持久事实。
    /// 凡是参与选招裁决或服务端掷骰得到的量都在「事实」区,并随 SendExtraAI 过线;
    /// 「声明」区每帧由 <see cref="BeginFrameDefaults"/> 清回安全默认值,漏声明的通道回落到无害状态
    /// </summary>
    public class VDStateContext : INpcStateContext
    {
        #region 核心引用
        public NPC Npc { get; set; }
        public Player Target { get; set; }
        public VoidDestroyer Owner { get; set; }
        /// <summary>目标存活且在感知距离内</summary>
        public bool TargetValid { get; set; }
        #endregion

        #region 事实:出招编排(权威端裁决,随包过线)
        /// <summary>阶段 1~3,映射 ai[2] 同步槽</summary>
        public int Phase
        {
            get => Math.Max(1, (int)Npc.ai[2]);
            set => Npc.ai[2] = value;
        }
        /// <summary>轮换出招序号(hub 沿表推进)</summary>
        public int AttackIndex { get; set; }
        /// <summary>出招冷却(hub 喘息)</summary>
        public int AttackCooldown { get; set; }
        /// <summary>连击队列:收招后直接接的状态号(-1 无)</summary>
        public int QueuedChainState { get; set; } = -1;
        /// <summary>阶段签名首招:转阶段收尾写入,hub 下一手强制取它(-1 无)</summary>
        public int ForcedNextState { get; set; } = -1;
        /// <summary>最近出过的招(环形,新在 0),防复读的硬判据;-1 为空位</summary>
        public int[] RecentHistory { get; } = { -1, -1, -1 };
        /// <summary>上一手的家族(同家族不相邻)</summary>
        public VDAttackFamily LastFamily { get; set; }
        /// <summary>死亡演出已开始(CheckDead 拦住真死)</summary>
        public bool Dying { get; set; }
        /// <summary>死亡演出已完,CheckDead 据此放行</summary>
        public bool DeathPerformanceFinished { get; set; }
        /// <summary>冲击帧单发闸:整场只用一次</summary>
        public bool ImpactFrameUsed { get; set; }
        #endregion

        #region 事实:服务端掷骰(随包过线)
        /// <summary>悬停/传送锚点</summary>
        public Vector2 AnchorPos { get; set; }
        /// <summary>四角索引 0 左上 1 右上 2 左下 3 右下</summary>
        public int CornerIndex { get; set; }
        /// <summary>侧向 ±1</summary>
        public int SideDir { get; set; } = 1;
        /// <summary>掷骰数量(如虚空火焰的 9~11)</summary>
        public int RandCount { get; set; }
        /// <summary>掷骰角度槽(裂隙方向等)</summary>
        public float[] RolledAngles { get; } = new float[4];
        /// <summary>掷骰落点槽(轨道轰炸目标等)</summary>
        public Vector2[] RolledPoints { get; } = new Vector2[6];
        /// <summary>切技闪现计时,>0 正在闪现:前半淡出,过半换位,后半淡入</summary>
        public int BlinkTimer { get; set; }
        /// <summary>传送后的无接触伤害窗口</summary>
        public int NoContactTimer { get; set; }
        /// <summary>当前承伤减免</summary>
        public float DamageReduction { get; set; } = VDDirector.DRPhase12;
        #endregion

        #region 声明:运动(每帧回落 Hold)
        public VDMoveMode Mode { get; set; }
        public Vector2 MoveTarget { get; set; }
        public float MoveSpeed { get; set; }
        public float Accel { get; set; } = 0.1f;
        public float SlowRadius { get; set; } = 120f;
        public Vector2 HoldOffset { get; set; }
        public float Stiffness { get; set; } = 0.12f;
        /// <summary>倾斜覆盖(弧度;NaN = 跟横向速度走)</summary>
        public float TiltOverride { get; set; } = float.NaN;
        #endregion

        #region 声明:判定
        /// <summary>本帧接触伤害窗(每帧回落 false,由状态或 ContactByDefault 打开)</summary>
        public bool ContactWindow { get; set; }
        #endregion

        #region 声明:表现(每帧重声明或自衰减,纯本地推导,不过线)
        /// <summary>核心亮度 0..1(自衰减)</summary>
        public float CoreGlow { get; set; }
        /// <summary>能量翼张开脉冲 0..1(自衰减)</summary>
        public float WingPulse { get; set; }
        /// <summary>全身抖动 0..1(纯绘制偏移,自衰减)</summary>
        public float ShakeStrength { get; set; }
        /// <summary>透明度声明(NaN = 未声明,宿主拉回 1)</summary>
        public float AlphaDeclared { get; set; } = float.NaN;
        /// <summary>绘制缩放声明(随 AlphaDeclared 一起)</summary>
        public float DrawScaleDeclared { get; set; } = 1f;
        /// <summary>假 Z 深度 0..1(轨道轰炸退入背景:缩小 + 蓝移 + 无接触)</summary>
        public float FakeZ { get; set; }
        /// <summary>核心目标色(全息三模式换色)</summary>
        public Color CoreColorTarget { get; set; } = VDVfx.VoidPurple;
        /// <summary>全息红恶魔蓄力读数 0..1(红色地狱状态每帧声明,弹幕只读)</summary>
        public float HoloCharge { get; set; }
        /// <summary>全息招式进入收尾(全息弹幕提前渐隐)</summary>
        public bool HoloWrapUp { get; set; }
        /// <summary>护盾可见(P3 起)</summary>
        public bool ShieldVisible { get; set; }
        /// <summary>能量翼可见(P2 起,变形/死亡时收起)</summary>
        public bool WingsVisible { get; set; }
        /// <summary>传送门开合 0..1(出场/死亡演出每帧声明,画在 AnchorPos)</summary>
        public float PortalOpenness { get; set; }
        /// <summary>变形动画帧(-1 = 不在变形)</summary>
        public int TransformFrame { get; set; } = -1;
        /// <summary>红射线预警进度 0..1(红色地狱预警窗声明)</summary>
        public float RedRayWarning { get; set; }
        /// <summary>导引线(主炮扫射起点预告):方向,零向量 = 无</summary>
        public Vector2 AimLineDir { get; set; }
        public float AimLineStrength { get; set; }
        public Color AimLineColor { get; set; } = VDVfx.CannonCore;
        /// <summary>限制圈是否绘制/生效(出场、死亡、撤离时关)</summary>
        public bool ArenaActive { get; set; }
        /// <summary>相机聚焦(出场演出):NaN 分量 = 不聚焦</summary>
        public Vector2 CameraFocus { get; set; } = new Vector2(float.NaN);
        public float CameraShift { get; set; }
        #endregion

        /// <summary>每帧默认值:运动回 Hold、判定关窗、表现量自然衰减</summary>
        public void BeginFrameDefaults()
        {
            Mode = VDMoveMode.Hold;
            MoveSpeed = 0f;
            Accel = 0.1f;
            SlowRadius = 120f;
            Stiffness = 0.12f;
            TiltOverride = float.NaN;

            ContactWindow = false;

            AlphaDeclared = float.NaN;
            DrawScaleDeclared = 1f;
            FakeZ = 0f;
            HoloCharge = 0f;
            HoloWrapUp = false;
            CoreColorTarget = VDVfx.VoidPurple;
            ShieldVisible = Phase >= 3 && !Dying;
            WingsVisible = Phase >= 2 && !Dying;
            PortalOpenness = 0f;
            TransformFrame = -1;
            RedRayWarning = 0f;
            AimLineDir = Vector2.Zero;
            AimLineStrength = 0f;
            AimLineColor = VDVfx.CannonCore;
            ArenaActive = true;
            CameraFocus = new Vector2(float.NaN);
            CameraShift = 0f;

            CoreGlow = MathHelper.Lerp(CoreGlow, 0f, 0.08f);
            if (CoreGlow < 0.01f)
            {
                CoreGlow = 0f;
            }
            WingPulse *= 0.94f;
            if (WingPulse < 0.02f)
            {
                WingPulse = 0f;
            }
            ShakeStrength *= 0.82f;
            if (ShakeStrength < 0.02f)
            {
                ShakeStrength = 0f;
            }
        }

        /// <summary>把一手招写进历史环(新在 0)</summary>
        public void PushHistory(VDStateIndex state)
        {
            for (int i = RecentHistory.Length - 1; i > 0; i--)
            {
                RecentHistory[i] = RecentHistory[i - 1];
            }
            RecentHistory[0] = (int)state;
        }

        public bool InHistory(VDStateIndex state)
        {
            int id = (int)state;
            for (int i = 0; i < RecentHistory.Length; i++)
            {
                if (RecentHistory[i] == id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
