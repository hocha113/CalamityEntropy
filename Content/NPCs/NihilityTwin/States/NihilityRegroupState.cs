using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 整备(原 <c>aitype == -1</c>)。两端各自收拢、本体扑向玩家,81 帧后随机选下一手。
    /// <para>
    /// 一阶段细胞只在离本体 120 以外才被往回拽;二阶段改成直接扑玩家,所以二阶段的"喘息段"
    /// 本身就是压力段。原代码如此。
    /// </para>
    /// <para>
    /// 计时在块首自增,所以判定看到的是「本帧是第几帧」,与基类那次尾随自增错开一格:
    /// <c>Num1</c> 走到 81 时收手,整备动作一共跑满 81 帧
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.Regroup, typeof(NihilityStateContext))]
    public class NihilityRegroupState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.Regroup;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            ctx.Num1++;
            npc.velocity *= NihilityDirector.RegroupDrag;
            cell.velocity *= NihilityDirector.RegroupDrag;
            if (ctx.Phase == 1)
            {
                if (CEUtils.getDistance(cell.Center, npc.Center) > NihilityDirector.RegroupCellLeash)
                {
                    cell.velocity += (npc.Center - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.RegroupCellPullP1;
                }
            }
            else
            {
                cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.RegroupCellPushP2;
            }
            npc.velocity += (targetPos - npc.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.RegroupThrust;
            npc.rotation = npc.velocity.ToRotation();
            ctx.Owner.SpawnParticle(ctx.Owner.buttom);

            if (ctx.Num1 > NihilityDirector.RegroupFrames)
            {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
