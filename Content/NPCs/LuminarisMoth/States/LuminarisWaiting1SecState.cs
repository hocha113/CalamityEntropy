using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 空拍一秒。原代码这一招的整个招式体<b>只有一句</b>把朝向掰正,
    /// 速度既不清零也不加力,所以本体带着上一手的残余速度滑行 92 帧(90 + 到 -1 的两帧)
    /// </summary>
    [VaultState((int)LuminarisStateIndex.Waiting1Sec, typeof(LuminarisStateContext))]
    public class LuminarisWaiting1SecState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.Waiting1Sec;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx) {
            int c = ctx.Countdown;
            ctx.Npc.rotation = 0;
            return Tick(ctx, c);
        }
    }
}
