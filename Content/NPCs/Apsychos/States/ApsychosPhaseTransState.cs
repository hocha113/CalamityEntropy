using CalamityEntropy.Content.NPCs.Apsychos.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 转阶段:前 80 帧白化涨满,过 80 帧置阶段 2,120 帧收。
    /// 关掉 HighLight 的默认衰减(原代码把衰减写成挂在本状态上的 else,所以只有这里不衰减)
    /// </summary>
    [VaultState((int)ApsychosStateIndex.PhaseTrans, typeof(ApsychosStateContext))]
    public class ApsychosPhaseTransState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.PhaseTrans;

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx)
        {
            NPC npc = ctx.Npc;
            npc.velocity *= ApsychosDirector.TransDrag;
            ctx.DecayHighLight = false;

            if (Timer < ApsychosDirector.TransRampFrames)
            {
                ctx.HighLight += 1f / ApsychosDirector.TransRampFrames;
                ctx.P2Lerp += 1f / ApsychosDirector.TransRampFrames;
            }
            else
            {
                ctx.HighLight *= ApsychosDirector.HighLightDecay;
                ctx.P2Lerp = 1f;
            }
            if (Timer > ApsychosDirector.TransRampFrames)
            {
                if (ctx.Phase != 2)
                {
                    ctx.Phase = 2;
                    MarkNetUpdate(ctx);
                }
            }
            if (Timer > ApsychosDirector.TransDuration)
            {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
