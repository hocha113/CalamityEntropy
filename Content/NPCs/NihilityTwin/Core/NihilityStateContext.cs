using CalamityEntropy.Core.AI;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.Core
{
    /// <summary>
    /// 虚无双子的状态上下文。
    /// <para>
    /// 「事实」区是参与判定或需要两端一致的量,随 <c>SendExtraAI</c> 过线;
    /// 「声明」区每帧由 <see cref="BeginFrameDefaults"/> 回落;
    /// 「表现」区是纯本地推导的绘制量,不过线。
    /// </para>
    /// <para>
    /// <b>关于 <see cref="Num1"/></b>:原代码十五段分支共用一个 <c>aicounter</c>,但每段自增的位置各不相同
    /// (有的在块首、有的在块尾、有的带条件、二阶段 0/2 号还是「冲刺次数」而不是帧计时),
    /// 所以它没法折进基类那一次固定的 <c>Timer++</c>。这里把它单独留成一个过线标量,
    /// 状态里逐字照抄原自增位置;<see cref="VaultState{TContext}.Timer"/> 退回纯状态帧龄,
    /// 只服务于线上计时与超时兜底(它每帧恒定 +1,反而是更准的帧差估计源)。
    /// </para>
    /// </summary>
    public class NihilityStateContext : CEBossStateContext
    {
        #region 核心引用
        public NihilityActeriophage Owner { get; set; }

        /// <summary>混沌细胞实体。可能为 null(尚未生成或已死),状态里读之前宿主已保证非空且存活</summary>
        public NPC Cell => Owner?.cell;
        #endregion

        #region 事实:过线
        /// <summary>
        /// 原 <c>aicounter</c>。各状态含义不同:多数是本段帧计时,
        /// 二阶段 0/2 号是<b>已完成的冲刺次数</b>,二阶段 1 号在蓄力期会每两帧多跳一格。
        /// 选招(原 <c>randomAI</c> / <c>prepareAiChange</c>)时无条件清零
        /// </summary>
        public int Num1 { get; set; }

        /// <summary>
        /// 原 <c>NPC.ai[2]</c>。一阶段 6 号拿它存环射基准角,二阶段 0/2 号拿它当冲刺子计时。
        /// 两处共用一个槽是原代码的写法,<b>照搬</b>:换招时不清零,残值会原样带进下一手
        /// </summary>
        public float Num2 { get; set; }

        /// <summary>
        /// 原 <c>NPC.ai[3]</c>。二阶段 1 号在发射帧锁存的服向,之后 30 帧细胞按它反向定速飞行。
        /// 新骨架里 <c>ai[3]</c> 归状态号占用,所以它挪成上下文字段并随包过线
        /// </summary>
        public float Num3 { get; set; }

        /// <summary>
        /// 原 <c>nz</c>。一阶段 2 号在蓄力期逐帧重算、突刺期只读的锁存突进向量。
        /// 它会反过来扰动本体速度(<c>NPC.velocity -= j * 0.01f</c>),所以必须过线
        /// </summary>
        public Vector2 Nz { get; set; }

        /// <summary>
        /// 原 <c>rotSpeed</c>。逐帧积分的自旋角速度,会累进写进 <c>NPC.rotation</c>,
        /// 而朝向又反过来决定细胞挂点与本体推进方向,属于典型的持久累加量,必须过线
        /// </summary>
        public float RotSpeed { get; set; }

        /// <summary>
        /// 原 <c>NPC.ai[0]</c>:一阶段 0 号的追击窗倒计时。<b>仍然住在 <c>ai[0]</c></b>,
        /// 原版同步槽白送一次同步,不必再进 ExtraAI
        /// </summary>
        public float ChaseTimer
        {
            get => Npc == null ? 0f : Npc.ai[0];
            set
            {
                if (Npc != null)
                {
                    Npc.ai[0] = value;
                }
            }
        }

        /// <summary>
        /// 原 <c>counter</c>(<c>NPC.ai[1]</c>):永不归零的全局帧计数,一堆 <c>counter % N</c> 射速门都读它。
        /// 走原版同步槽,两端天然一致
        /// </summary>
        public int FrameCounter
        {
            get => Npc == null ? 0 : (int)Npc.ai[1];
            set
            {
                if (Npc != null)
                {
                    Npc.ai[1] = value;
                }
            }
        }
        #endregion

        #region 事实:每帧重算(各端同算,不过线)
        /// <summary>目标距离,每帧由宿主重算</summary>
        public float TargetDistance { get; set; }
        #endregion

        #region 声明:每帧回落
        /// <summary>
        /// 本帧是否保留 <see cref="RotSpeed"/>。原代码把清零写成挂在「aitype == 1」上的 <c>else</c>
        /// (一阶段那句还带 <c>aitype != 4</c> 的豁免),效果就是「除自旋类招式外每帧清零」,
        /// 所以只有一阶段 1/4 号与二阶段 1 号声明它
        /// </summary>
        public bool KeepRotSpeed { get; set; }
        #endregion

        /// <summary>
        /// 每帧默认值。只回落 <see cref="KeepRotSpeed"/>——其余都是持久事实,
        /// 原代码也不在换招时清(<c>prepareAiChange</c> 只动 <c>aicounter</c> 与 <c>aitype</c>)
        /// </summary>
        public override void BeginFrameDefaults()
        {
            base.BeginFrameDefaults();
            KeepRotSpeed = false;
        }
    }
}
