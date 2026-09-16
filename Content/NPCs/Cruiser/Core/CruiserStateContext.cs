using CalamityEntropy.Core.AI;

namespace CalamityEntropy.Content.NPCs.Cruiser.Core
{
    /// <summary>
    /// 巡游者状态上下文。
    /// <para>
    /// 「事实」区是参与判定或需要两端一致的量,随 <c>SendExtraAI</c> 过线;
    /// 「声明」区每帧由 <see cref="BeginFrameDefaults"/> 回落;
    /// 「表现」区是纯本地推导的绘制量,不过线(状态号、计时与各累加量已过线,它们自然收敛)。
    /// </para>
    /// </summary>
    public class CruiserStateContext : CEBossStateContext
    {
        #region 核心引用
        public CruiserHead Owner { get; set; }

        /// <summary>当前状态号。读的是已同步的 <c>ai[3]</c>,所以两端同值(尾鞭新星要按当前招削弱)</summary>
        public CruiserStateIndex StateIndex => Npc == null ? CruiserStateIndex.TryToClosePlayer : (CruiserStateIndex)(int)Npc.ai[3];
        #endregion

        #region 事实:过线
        /// <summary>
        /// 原 <c>CruiserHead.changeCounter</c>。<b>跨状态持久</b>,只在选招(原 <c>changeAi()</c>)时清零。
        /// <para>
        /// 它<b>不能</b>换成状态自己的 <c>Timer</c>:原代码有三处不能用状态龄表达的写法——
        /// 转阶段整段不推进它、咬击的「等咬中」窗口停在 0、激光的瞄准窗不推进它,
        /// 而且转阶段收尾直接置 <c>ai = VoidSpike</c> 时<b>不</b>调 <c>changeAi()</c>,
        /// 于是二阶段第一手尖刺是带着上一手的残值起跑的(详见 <see cref="CruiserRotation"/> 注释)。
        /// </para>
        /// </summary>
        public int ChangeCounter { get; set; }

        /// <summary>
        /// 原 <c>NPC.localAI[2]</c>:虚空激光的瞄准窗计时。
        /// localAI 不随原版快照过线,所以它在原代码里是一处未同步量;本轮改成随包过线。
        /// 只由激光状态读写,并在激光收招时清零(原代码也在那一处清)
        /// </summary>
        public int LaserAim { get; set; }
        #endregion

        #region 事实:每帧重算(各端同算,不过线)
        /// <summary>目标距离,每帧由宿主重算</summary>
        public float TargetDistance { get; set; }
        #endregion

        #region 声明:每帧回落
        /// <summary>
        /// 本帧请求一次尾鞭(原 <c>tjv = 1</c>)。原代码里它在同一帧就被尾鞭段消费掉,
        /// 所以这里做成每帧回落的声明通道,不是跨帧闩锁
        /// </summary>
        public bool TailWhipCue { get; set; }
        #endregion

        #region 表现:纯本地
        /// <summary>
        /// 嘴部张角(原 <c>mouthRot</c>)。纯绘制量:判定盒与伤害都不看它。
        /// 下限钳在 <see cref="CruiserDirector.MouthMin"/>
        /// </summary>
        public float MouthRot { get; set; }

        /// <summary>咬合闩锁(原 <c>bite</c>)。同样只影响嘴的绘制</summary>
        public bool Biting { get; set; }
        #endregion

        /// <summary>
        /// 每帧默认值。只回落尾鞭声明——<see cref="ChangeCounter"/> / <see cref="LaserAim"/>
        /// 是跨状态持久量,嘴部两项由宿主在状态机之后统一结算,都不在这里动
        /// </summary>
        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
            TailWhipCue = false;
        }
    }
}
