using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.States
{
    /// <summary>
    /// 落环喷泉。本体同样只收柱子,魂环抛飞落地、互相推开、蓄力后放冲击波
    /// (全套写在 <see cref="SpiritRing"/> 里)。
    /// <para>
    /// 本段 280 帧,收招回横扫。<b>这是全链唯一一次「换回更靠前的状态」</b>:
    /// 原代码的横扫块写在本块<b>上方</b>,所以横扫要等下一帧才跑,而那一帧开头的统一自增
    /// 会让它从 1 起跑。宿主的续跑链按 <see cref="SpiritFountainRotation.ChainOrder"/> 判出这一点,
    /// 用 <see cref="SpiritFountainStateBase.AdoptDeferredEntry"/> 补上那次自增。
    /// </para>
    /// </summary>
    [VaultState((int)SpiritFountainStateIndex.RingFountains, typeof(SpiritFountainStateContext))]
    public class SpiritFountainRingFountainsState : SpiritFountainStateBase
    {
        public override SpiritFountainStateIndex StateIndex => SpiritFountainStateIndex.RingFountains;

        protected override IVaultState<SpiritFountainStateContext> RunBody(SpiritFountainStateContext ctx)
        {
            ctx.Owner.column1.offset.X *= SpiritFountainDirector.ColumnRetractDamp;
            if (Timer > SpiritFountainDirector.RingFountainsDuration)
            {
                return Advance(ctx, StateIndex);
            }
            return null;
        }
    }
}
