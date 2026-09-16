using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.Core
{
    /// <summary>
    /// 选招:原 <c>NihilityActeriophage.prepareAiChange()</c> 与 <c>randomAI()</c> 逐条搬过来。
    /// <para>
    /// <b>这只 Boss 的出招本来就是随机的</b>,不是手排表。所以这里<b>不引入防复读阀</b>:
    /// 七面均匀掷点、外加原有的那一条抑制(二阶段掷到「分裂增殖」且场上小细胞超过 8 只就重掷),
    /// 一个字不多一个字不少。加查重窗会改变出招分布,那是手感改动,不在本次无损迁移的范围内。
    /// </para>
    /// <para>
    /// <b>掷点收归权威端。</b>原代码的 <c>randomAI()</c> 在客户端也会跑一遍(它没有任何 netMode 门),
    /// 各端各掷各的,靠 ExtraAI 里的 <c>aitype</c> 覆盖回来;新骨架里门开在
    /// <see cref="NihilityStateBase.NextAttack"/> / <see cref="NihilityStateBase.EndAttack"/> 上,
    /// 结果经 <c>ai[3]</c> 状态号过线。
    /// </para>
    /// </summary>
    public static class NihilityRotation
    {
        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        public static IVaultState<NihilityStateContext> Create(NihilityStateIndex state)
            => VaultStateRegistry<NihilityStateContext>.Create((int)state);

        /// <summary>
        /// 收招回整备(原 <c>prepareAiChange</c>)。只清 <c>aicounter</c>,
        /// <b>不</b>清 <see cref="NihilityStateContext.Num2"/> / <see cref="NihilityStateContext.Num3"/> /
        /// <see cref="NihilityStateContext.Nz"/> / <see cref="NihilityStateContext.ChaseTimer"/>——
        /// 原代码也不清,残值会原样带进下一手
        /// </summary>
        public static IVaultState<NihilityStateContext> Regroup(NihilityStateContext ctx) {
            ctx.Num1 = 0;
            return Create(NihilityStateIndex.Regroup);
        }

        /// <summary>
        /// 随机选下一手(原 <c>randomAI</c>)。<b>只该由权威端调用</b>:它带副作用(清计数、吃随机数)。
        /// 抑制条件与原代码同构,包括「重掷」是整段重来而不是换一个面
        /// </summary>
        public static IVaultState<NihilityStateContext> Pick(NihilityStateContext ctx) {
            ctx.Num1 = 0;
            int roll = Main.rand.Next(NihilityDirector.AttackRollFaces);
            if (ctx.Phase == 2 && roll == NihilityDirector.SplitRoll
                && NihilityDirector.CountSmallCells(ModContent.NPCType<ChaoticCellSmall>()) > NihilityDirector.SpawnCellCap) {
                //原代码在这里递归调用 randomAI():整段重掷,而不是在剩下六面里挑
                return Pick(ctx);
            }
            return Create(NihilityDirector.StateFor(ctx.Phase, roll));
        }
    }
}
