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
        public int Phase
        {
            get => Npc == null ? 1 : Math.Max(1, (int)Npc.ai[2]);
            set
            {
                if (Npc != null)
                {
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
        /// 每帧默认值:把全部声明通道清回安全默认,包络量自衰减。
        /// 子类覆写时先调 <c>base.BeginFrameDefaults()</c>
        /// </summary>
        public virtual void BeginFrameDefaults()
        {
        }
    }
}
