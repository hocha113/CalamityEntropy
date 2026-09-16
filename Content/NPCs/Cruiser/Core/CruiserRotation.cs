using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.Core
{
    /// <summary>
    /// 选招:原 <c>CruiserHead.changeAi()</c> 的裁决顺序逐条搬过来。
    /// <para>
    /// <b>不引入防复读阀。</b>两张表是作者手排的序列,全局只有两处骰点(一阶段 6/19 号槽的
    /// 能量球/残渣二选一、巡航的每帧 1/150 收招概率),加查重窗只会改变出招序列,
    /// 那是手感改动,不在本次无损迁移的范围内。
    /// </para>
    /// <para>
    /// 四条容易被"顺手修掉"的隐式行为,都必须保留:
    /// <list type="number">
    /// <item>一阶段 6 号与 19 号槽是<b>哨兵</b>:上一手若是「拉开」,就把序号<b>退一格</b>并改出「直扑」,
    /// 于是下一次选招又落回同一个哨兵槽,此时上一手必然是直扑,才真正骰出能量球或残渣。
    /// 效果是「哨兵槽永远跟在一手直扑之后」,而且那一格会被走两次</item>
    /// <item>二阶段每次选招都<b>重新压一遍</b> <c>NPC.defense = 50</c> 与 <c>DamageReduction = 0.42</c>,
    /// 不是只在转阶段压一次</item>
    /// <item>转阶段收尾<b>不走</b>这里:原代码直接写 <c>ai = VoidSpike</c>,既不清
    /// <c>changeCounter</c> 也不动 <c>aiRound</c>。所以二阶段第一手尖刺是带着被打断那一手的
    /// 残余计数起跑的——残值只要超过 150,这一手尖刺会在第一帧就直接收招(一发尖刺都不放),
    /// 二阶段实际开场变成 1 号槽的布雷。<b>这是原版行为,照搬</b></item>
    /// <item>阶段判据读的是<b>当帧</b>的阶段(由血量每帧重算),不是进状态时的快照</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class CruiserRotation
    {
        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        public static IVaultState<CruiserStateContext> Create(CruiserStateIndex state)
            => VaultStateRegistry<CruiserStateContext>.Create((int)state);

        /// <summary>
        /// 选下一手。<b>只该由权威端调用</b>:它带副作用(清计数、推进序号、压护甲与减伤),
        /// 而 <see cref="VaultStateMachine{TContext}"/> 在客户端会照常跑 <c>OnUpdate</c> 却丢弃返回值。
        /// 门开在 <see cref="CruiserStateBase.NextAttack"/> 上
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="from">正在收招的状态。一阶段哨兵槽要读它(原代码读的是当时还没改的 <c>ai</c>)</param>
        public static IVaultState<CruiserStateContext> Pick(CruiserStateContext ctx, CruiserStateIndex from)
        {
            //对齐原代码:计数先无条件清零,再做裁决(netUpdate 由 AiSlotNetSync 在写状态号时打)
            ctx.ChangeCounter = 0;

            NPC npc = ctx.Npc;
            if (ctx.Phase == 1)
            {
                ctx.AttackIndex++;
                if (ctx.AttackIndex > CruiserDirector.Phase1.Length - 1)
                {
                    ctx.AttackIndex = 0;
                }
                CruiserStateIndex slot = CruiserDirector.Phase1[ctx.AttackIndex];
                if (slot == CruiserStateIndex.PhaseTransing)
                {
                    //隐式行为一:哨兵槽
                    if (from == CruiserStateIndex.StayAwayAndShootVoidStar)
                    {
                        ctx.AttackIndex--;
                        slot = CruiserStateIndex.TryToClosePlayer;
                    }
                    else
                    {
                        slot = Main.rand.NextBool() ? CruiserStateIndex.EnergyBall : CruiserStateIndex.VoidResidue;
                    }
                }
                return Create(slot);
            }

            //隐式行为二:二阶段每次选招都重压护甲与减伤
            npc.defense = CruiserDirector.DefensePhase2;
            ctx.Owner.DamageReduction = CruiserDirector.DRPhase2;
            ctx.AttackIndex++;
            if (ctx.AttackIndex >= CruiserDirector.Phase2.Length)
            {
                ctx.AttackIndex = 0;
            }
            return Create(CruiserDirector.Phase2[ctx.AttackIndex]);
        }
    }
}
