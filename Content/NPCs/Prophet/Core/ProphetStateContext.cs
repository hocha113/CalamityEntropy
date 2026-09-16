using CalamityEntropy.Core.AI;

namespace CalamityEntropy.Content.NPCs.Prophet.Core
{
    /// <summary>
    /// 先知状态上下文。
    /// <para>
    /// 「事实」区是参与判定或需要两端一致的量,随 <c>SendExtraAI</c> 过线;
    /// 「每帧重算」区各端同算所以不过线;表现量留在宿主上(尾迹、鳍相位、绘制朝向),
    /// 它们全由已过线的位置/朝向推导,自己会收敛。
    /// </para>
    /// <para>
    /// 先知没有「声明通道」:原代码的每个招式都直接写 <c>NPC.velocity</c> / <c>NPC.rotation</c>,
    /// 无损迁移就照这个形状搬,不另造一层运动模式枚举。
    /// 所以 <see cref="BeginFrameDefaults"/> 只重声明目标引用,没有要回落的包络量
    /// </para>
    /// </summary>
    public class ProphetStateContext : CEBossStateContext
    {
        public ProphetStateContext() {
            //原 AIC 字段初值是 -1,所以第一次选招自增后正好落在 0 号槽(第一手必定是四轮符文弹)。
            //基类的 AttackIndex 默认 0,不在这里扳回来的话开场第一手会变成 3 号招
            AttackIndex = ProphetDirector.AttackIndexStart;
        }

        #region 核心引用
        public TheProphet Owner { get; set; }
        #endregion

        #region 事实:过线
        /// <summary>
        /// 原 <c>AIChangeDelay</c>:<b>倒计时</b>,不是正计时。
        /// 选招那一帧被赋上本招时长,状态体当帧就以满值跑一遍,帧末自减一次。
        /// <para>
        /// 所有节拍都对它做<b>原样判定</b>(<c>== 88</c>、<c>% 60 == 30</c>、<c>&gt; 480</c> …),
        /// 没有翻成正计时阈值。它随包过线并带 ±2 容差收养,
        /// 所以大激光那一处 <c>倒计时 = 30</c> 的中途压缩也会原样传到客户端。
        /// </para>
        /// <para>
        /// 基类的 <c>Timer</c> / <c>Counter</c> 仍然照跑,只当收养通道与超时兜底,不参与节拍
        /// </para>
        /// </summary>
        public int Countdown { get; set; }

        /// <summary>
        /// 原 <c>spawnAnm</c>:出生演出倒计时,归零后<b>继续往负数走</b>(原代码没有下限)。
        /// 大于 0 时不出招、不更新尾迹、强制朝上且免疫伤害。
        /// 过线是为了让中途加入的客户端不再从 120 重跑一遍出生演出(那两秒里它会完全停摆)
        /// </summary>
        public int SpawnAnim { get; set; } = ProphetDirector.SpawnAnimFrames;

        /// <summary>
        /// 原 <c>dr</c>:开场额外减伤,每帧自减直到 0。
        /// 它是逐帧积分量,不过线的话中途加入的客户端会一直按 0.26 的起始值算伤害
        /// </summary>
        public float DrRamp { get; set; } = ProphetDirector.DrRampInitial;

        /// <summary>
        /// 瞬移流水号。权威端每完成一次瞬移就 +1,客户端据此把一次位置跳变认成瞬移而不是失步:
        /// 清尾迹、补放两端火花、丢掉本地预测
        /// </summary>
        public int TeleportSeq { get; set; }

        /// <summary>权威端掷骰算出的瞬移落点。与流水号一起过线,客户端补演出时用它定位</summary>
        public Vector2 TeleportPos { get; set; }
        #endregion

        #region 事实:每帧重算(各端同算,不过线)
        /// <summary>
        /// 原 <c>difficult</c>:难度系数,宿主每帧在状态机之前重算。
        /// 六个开关全是世界级已同步量,血量比例读已同步的 <c>NPC.life</c>,所以各端同值。
        /// 它直接乘进速度与瞬移半径的分母,任何一端算出不同值都会让位置误差无界增长
        /// </summary>
        public float Difficult { get; set; } = 1f;
        #endregion

        /// <summary>先知没有需要回落的声明通道,这里只保持契约形状</summary>
        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
        }
    }
}
