using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.States
{
    /// <summary>
    /// 转阶段演出。血量跌破 66% 时由宿主无条件插入(不等当前招打完),一号柱归零、二号柱亮起,
    /// 期间魂环整体免伤。
    /// <para>
    /// <b>本状态每帧额外自增一次计时</b>(原代码在块内写了第二次 <c>aiTimer++</c>),
    /// 所以计时走的是 1、3、5…,140 帧的门槛实际只要 71 帧就撞到。照搬,不要顺手删掉那次自增。
    /// </para>
    /// <para>联机:免伤开关等价于「当前状态是不是本状态」,而状态号走 ai[3],不必单独过线。</para>
    /// </summary>
    [VaultState((int)SpiritFountainStateIndex.PhaseTranse1, typeof(SpiritFountainStateContext))]
    public class SpiritFountainPhaseTranse1State : SpiritFountainStateBase
    {
        public override SpiritFountainStateIndex StateIndex => SpiritFountainStateIndex.PhaseTranse1;

        protected override IVaultState<SpiritFountainStateContext> RunBody(SpiritFountainStateContext ctx) {
            SpiritFountain owner = ctx.Owner;

            ctx.DontTakeDmg = true;
            owner.column1.offset *= 0;
            owner.column2.alpha = float.Lerp(owner.column2.alpha, SpiritFountainDirector.TransColumn2Alpha, SpiritFountainDirector.TransColumn2AlphaLerp);
            owner.column2.id = 1;
            //块内的第二次自增:基类在状态体之后还会再加一次,合起来每帧 +2
            Timer++;

            if (Timer > SpiritFountainDirector.TransDuration) {
                ctx.DontTakeDmg = false;
                IVaultState<SpiritFountainStateContext> next = Advance(ctx, StateIndex);
                owner.column2.alpha = SpiritFountainDirector.TransColumn2Alpha;
                return next;
            }
            return null;
        }
    }
}
