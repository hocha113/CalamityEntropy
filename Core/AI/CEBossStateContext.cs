using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Core.AI
{
    /// <summary>
    /// Boss 状态上下文基类:每帧声明总线 + 跨帧持久事实的公共部分。
    /// <para>
    /// 只收各 Boss 都要的那几样:实体引用、目标校验、阶段槽、轮换序号。
    /// 运动声明通道<b>不</b>放在这里——悬停飞行、转向推进、贴地爬行是身体尺度的差异,
    /// 强行抽象成一套模式枚举只会让每只 Boss 都用不顺手。各 Boss 在自己的上下文里加。
    /// </para>
    /// <para>
    /// <see cref="BeginFrameDefaults"/> 是硬性契约:每帧开头把所有声明通道清回安全默认值,
    /// 漏声明的通道回落到无害状态,而不是留着上一个状态的残值。新增通道时必须同步补默认值。
    /// </para>
    /// </summary>
    public abstract class CEBossStateContext : INpcStateContext
    {
        /// <summary>宿主 NPC</summary>
        public NPC Npc { get; set; }

        /// <summary>当前目标玩家(可能为 null 或已死亡,读之前看 <see cref="TargetValid"/>)</summary>
        public Player Target { get; set; }

        /// <summary>目标存活且在感知距离内</summary>
        public bool TargetValid { get; set; }

        /// <summary>
        /// 阶段,映射 <c>ai[2]</c> 同步槽,所以客户端不必额外过线就能读到。
        /// 下限钳到 1,免得生成首帧 ai 槽还是 0 时算出个 0 阶段
        /// </summary>
        public int Phase {
            get => Npc == null ? 1 : Math.Max(1, (int)Npc.ai[2]);
            set {
                if (Npc != null) {
                    Npc.ai[2] = value;
                }
            }
        }

        /// <summary>
        /// 轮换出招序号。参与选招裁决,所以只有权威端读写才算数,且必须随快照过线——
        /// 客户端靠它才能在中途加入时接着正确的位置走
        /// </summary>
        public int AttackIndex { get; set; }

        /// <summary>
        /// 出招倒计时。<b>倒计时型 Boss 的主时钟</b>,选招时置成本招时长,之后每帧自减一次,
        /// 跌破 0 就收招。递增型 Boss 不用它,留 0 即可。
        /// <para>
        /// 它是一等公民而不是各 Boss 自己加的槽,是因为原生 AI 里「倒计时 + 对它做
        /// <c>== N</c> / <c>&gt; N</c> / <c>% N == 0</c> 判定」这套写法太普遍:
        /// 把几十条阈值手工翻成 <c>Timer</c> 的递增写法,一个符号错就是静默的手感改动。
        /// 倒计时原样留着、节拍照原样判,是成本最低的无损迁移路径。
        /// </para>
        /// <para>
        /// <b>过线口径</b>:它逐帧积分且驱动全部节拍,必须随快照过线,
        /// 并按 <see cref="CEBossNetAdopt.AdoptFrameCounter(int, int, int)"/> 的帧计数口径收养。
        /// 拍点判据用 <see cref="CEBossStateBase{TCtx}.CuePassedDescending"/> 或
        /// <see cref="CEBossCue.TryFireDescending"/> 的<b>递减</b>版,递增版在这里全是反的。
        /// </para>
        /// <para>
        /// <b>相位约定(迁移时最容易出错的一条)。</b>原生 AI 普遍是「先自减计时器、再跑招式体」,
        /// 而 <see cref="CEBossStateBase{TCtx}"/> 的契约是「先跑体、再自增 <c>Timer</c>」,
        /// 招式体里的自减也就落到了体的末尾。判据只有一条:
        /// <b>状态体读到的是自减前的值还是自减后的值。</b>
        /// </para>
        /// <para>
        /// 读到自减<b>前</b>的值(原代码在体的开头就 <c>counter--</c>) → 本字段存的是
        /// 「体读到的值」,比原字段少一拍,<b>初值与一切外部写入都要减 1</b>。
        /// Luminaris 就是这一类:初值 -1 对应原字段的 0,天顶分身的 210~270 同样减 1;
        /// 漏掉这个 -1,生成首帧会多喷一发漩涛。
        /// </para>
        /// <para>
        /// 读到自减<b>后</b>的值,或者自减根本不在状态块里(魂泉是「AI 开头统一自增、再跑状态块」),
        /// 两侧读到的就是同一个值,<b>不减 1</b>。照着别的 Boss 抄 -1 反而会整体错一帧。
        /// </para>
        /// <para>
        /// 允许覆写,只是为了让各 Boss 在自己的上下文里挂专属的相位说明与初值;
        /// 覆写请写 <c>override</c> 而不是重新声明一个同名属性,
        /// 后者会遮蔽基类槽位,共用层按基类类型读到的永远是 0。
        /// </para>
        /// </summary>
        public virtual int Countdown { get; set; }

        /// <summary>
        /// 每帧默认值:把全部声明通道清回安全默认,包络量自衰减。
        /// 子类覆写时先调 <c>base.BeginFrameDefaults()</c>
        /// </summary>
        public virtual void BeginFrameDefaults() {
        }
    }
}
