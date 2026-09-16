using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.States
{
    /// <summary>
    /// 回旋。本体这一段几乎什么都不做:柱子收回中线轻晃、扶正,真正的威胁在魂环身上——
    /// 进度量 <see cref="SpiritFountainStateContext.Num1"/> 每帧上涨,魂环拿自己的 Index 跟它比大小,
    /// 于是一圈魂环<b>由内到外依次脱柱</b>扑向玩家。
    /// <para>
    /// 节拍:前 80 帧进度压着不涨(魂环还挂在柱上,这 80 帧就是预告);
    /// 之后每帧 +0.003(三阶段 +0.005),涨到 1.36(三阶段 1.66)收招进激光。
    /// </para>
    /// <para>
    /// 联机:进度量是部件也要读的判定量,过线。
    /// 柱子的轻晃读 <c>Main.GameUpdateCount</c>——那是各端独立的本地帧计数,原代码如此,见宿主里的说明。
    /// </para>
    /// </summary>
    [VaultState((int)SpiritFountainStateIndex.Boomerang, typeof(SpiritFountainStateContext))]
    public class SpiritFountainBoomerangState : SpiritFountainStateBase
    {
        public override SpiritFountainStateIndex StateIndex => SpiritFountainStateIndex.Boomerang;

        protected override IVaultState<SpiritFountainStateContext> RunBody(SpiritFountainStateContext ctx) {
            SpiritFountain owner = ctx.Owner;
            bool phase3 = ctx.Phase == SpiritFountainDirector.BoomerangPhase3;

            owner.column1.offset.X = float.Lerp(owner.column1.offset.X,
                (float)Math.Cos(Main.GameUpdateCount * SpiritFountainDirector.BoomerangIdleFreq) * SpiritFountainDirector.BoomerangIdleAmp,
                SpiritFountainDirector.BoomerangOffsetLerp);
            owner.column1.rotation = -MathHelper.PiOver2;

            if (Timer > SpiritFountainDirector.BoomerangChargeStartFrame) {
                ctx.Num1 += phase3 ? SpiritFountainDirector.BoomerangStepP3 : SpiritFountainDirector.BoomerangStep;
            }
            else {
                //前 80 帧每帧压回 0,而不是「不涨」:魂环读到的进度必须是 0
                ctx.Num1 = 0;
            }

            if (ctx.Num1 > (phase3 ? SpiritFountainDirector.BoomerangLimitP3 : SpiritFountainDirector.BoomerangLimit)) {
                //换态那一行顺手清进度,收招口不清(原代码就是在这里清的)
                ctx.Num1 = 0;
                return Advance(ctx, StateIndex);
            }
            return null;
        }
    }
}
