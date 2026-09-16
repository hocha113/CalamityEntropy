using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.States
{
    /// <summary>
    /// 激光。本体只负责把柱子的横向偏移收回中线,整段火力都在魂环的扫射上
    /// (瞄准、预警、发射全写在 <see cref="SpiritRing"/> 里,按本状态与 <c>aiTimer</c> 取模驱动)。
    /// <para>本段 460 帧,收招进落环喷泉。</para>
    /// </summary>
    [VaultState((int)SpiritFountainStateIndex.Lasers, typeof(SpiritFountainStateContext))]
    public class SpiritFountainLasersState : SpiritFountainStateBase
    {
        public override SpiritFountainStateIndex StateIndex => SpiritFountainStateIndex.Lasers;

        protected override IVaultState<SpiritFountainStateContext> RunBody(SpiritFountainStateContext ctx) {
            ctx.Owner.column1.offset.X *= SpiritFountainDirector.ColumnRetractDamp;
            if (Timer > SpiritFountainDirector.LasersDuration) {
                return Advance(ctx, StateIndex);
            }
            return null;
        }
    }
}
