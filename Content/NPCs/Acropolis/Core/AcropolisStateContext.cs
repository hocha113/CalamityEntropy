using CalamityEntropy.Core.AI;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis.Core
{
    /// <summary>
    /// 卫城机器的状态上下文。
    /// <para>
    /// 「事实」区是参与判定或需要两端一致的量,随 <c>SendExtraAI</c> 过线;
    /// 「每帧重算」区各端各算,值相同所以不过线;
    /// 「声明」区每帧由 <see cref="BeginFrameDefaults"/> 回落。
    /// </para>
    /// <para>
    /// 原代码没有互斥状态,靠一堆并行倒计时驱动。迁移时把<b>招式</b>抽成互斥状态,
    /// 把<b>跨招冷却、走路、腿部动画、鱼叉循环、拽拉</b>留在宿主里每帧跑——
    /// 它们本来就不属于任何一招,塞进状态反而会让本体在出招时僵住。
    /// </para>
    /// </summary>
    public class AcropolisStateContext : CEBossStateContext
    {
        #region 核心引用
        public AcropolisMachine Owner { get; set; }

        /// <summary>炮臂。出招时由状态声明瞄点,常态由宿主瞄玩家</summary>
        public AcropolisHand Cannon => Owner?.cannon;

        /// <summary>鱼叉臂。完全由宿主背景驱动,状态不碰</summary>
        public AcropolisHand HarpoonArm => Owner?.harpoon;
        #endregion

        #region 事实:过线
        /// <summary>
        /// 跨招冷却。宿主每帧按 enrange 扣,归零后由 <see cref="AcropolisRotation"/> 骰点并重置。
        /// <b>出招期间照常流逝</b>,所以招式频率与原代码一致,只是新的一招要排队
        /// </summary>
        public float TeslaCD { get; set; } = AcropolisDirector.TeslaCDInit;

        /// <summary>
        /// 炮口连射节拍。炮击与跳射共用同一个累加器,且<b>跨状态保留</b>——
        /// 原代码里它也不在任何一处被重置,只有起跳那一刻被写成 30
        /// </summary>
        public float TeslaUpCD { get; set; }

        /// <summary>鱼叉再装填冷却,只在鱼叉回到发射架上时流逝</summary>
        public float HarpoonCD { get; set; } = AcropolisDirector.HarpoonCDInit;

        /// <summary>鱼叉蓄力 0~1,满即发射。超过 0.8 后鱼叉臂停止追瞄</summary>
        public float HarpoonCharge { get; set; }

        /// <summary>
        /// 跳射剩余计数。原代码是 <c>JumpAndShoot</c>:跳射状态每帧自减,
        /// 落地后由宿主强制写回 -1。同时是落地闸(降到 150 以下才允许收招)
        /// </summary>
        public int JumpAndShoot { get; set; } = -1;

        /// <summary>
        /// 鱼叉拽拉倒计时。原代码借 <c>NPC.ai[2]</c> 存它,而 ai[2] 现在归阶段用,
        /// 所以挪成独立字段随包过线。鱼叉实体每帧把它刷成 2,所以只要卡着不放就一直拽
        /// </summary>
        public int PullTimer { get; set; }

        /// <summary>
        /// 单发电球的开火计数(回绕)。骰点只在权威端,结果必须过线——
        /// 各端靠它的变化补上本地的炮臂反冲与音效,否则远端客户端听不到这一声
        /// </summary>
        public byte ShotCue { get; set; }
        #endregion

        #region 事实:每帧重算(各端同算,不过线)
        /// <summary>
        /// 难度系数,每帧在状态机之前由宿主重算。来源全是世界级已同步量加上已同步的 life,
        /// 所以各端同值。它直接乘进速度与冷却流速,改动它的任何来源都要先确认同步性
        /// </summary>
        public float Enrange { get; set; } = 1f;

        /// <summary>目标距离,每帧重算</summary>
        public float TargetDistance { get; set; }

        /// <summary>着地腿数,每帧由腿组重算(腿的落点随包过线,所以各端同值)</summary>
        public int LegsOnTile { get; set; }

        /// <summary>站得住:三条腿着地,或本体盒子压到实心块</summary>
        public bool Grounded { get; set; }

        /// <summary>鱼叉是否在发射架上。走位、追高、鱼叉冷却都读它</summary>
        public bool HarpoonOnLauncher { get; set; }

        /// <summary>本体处于晋升后的战斗形态且有可打的目标。为假时状态体整体不跑</summary>
        public bool Engaged { get; set; }
        #endregion

        #region 声明:每帧回落
        /// <summary>
        /// 本帧炮口瞄点。<c>null</c> 表示没有状态声明,宿主落地成常态瞄准(玩家身位 + 抛物线补偿)。
        /// 对应原代码 <c>CannonUpAtk</c> / <c>JumpAndShoot</c> / <c>else</c> 那三选一
        /// </summary>
        public Vector2? CannonAim { get; set; }

        /// <summary>本帧对炮口调用几次追瞄。跳射原代码连调两次,等于转向速率翻倍</summary>
        public int CannonAimTimes { get; set; } = 1;
        #endregion

        #region 本地:不过线
        /// <summary>上一次本地消费到的开火计数,用来给 <see cref="ShotCue"/> 做边沿检测</summary>
        public byte LocalShotCue { get; set; }

        /// <summary>收到过至少一包,用来抑制中途加入时的假边沿</summary>
        public bool ShotCueSynced { get; set; }
        #endregion

        /// <summary>
        /// 每帧默认值。只回落两个炮口声明通道,其余都是持久事实,
        /// 由宿主在状态机之后统一结算
        /// </summary>
        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
            CannonAim = null;
            CannonAimTimes = 1;
        }

        /// <summary>目标玩家,读之前看 <see cref="CEBossStateContext.TargetValid"/></summary>
        public Player Player => Target;

        /// <summary>本体腾空中。原代码的 <c>Jumping</c>,持久量,住在宿主上给鱼叉与腿组读</summary>
        public bool Airborne {
            get => Owner != null && Owner.Jumping;
            set {
                if (Owner != null) {
                    Owner.Jumping = value;
                }
            }
        }

        /// <summary>朝向。原代码的 <c>dir</c>,翻转时会把 <c>NPC.rotation</c> 转半圈,所以是累加量</summary>
        public int Dir => Owner?.dir ?? 1;
    }
}
