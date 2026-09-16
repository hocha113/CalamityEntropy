using CalamityEntropy.Content.NPCs.Cruiser.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 巡航:稳速 40 追瞄,是二阶段唯一的喘息段。
    /// 100 帧后每帧 1/150 概率收招,200 帧硬收——<b>全模组唯一一处影响出招时机的骰点</b>,
    /// 所以只在权威端骰,客户端安静等换态包
    /// </summary>
    [VaultState((int)CruiserStateIndex.Cruise, typeof(CruiserStateContext))]
    public class CruiserCruiseState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.Cruise;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            Vector2 dir = (player.Center - npc.Center).normalize();

            npc.velocity = npc.velocity.normalize()
                * (npc.velocity.Length() + (CruiserDirector.CruiseSpeedTarget - npc.velocity.Length()) * CruiserDirector.CruiseSpeedLerp);
            npc.velocity += dir * CruiserDirector.CruiseThrust;
            npc.velocity = Vector2.Lerp(npc.velocity, dir * npc.velocity.Length(), CruiserDirector.CruiseAimLerp);
            npc.velocity *= CruiserDirector.CruiseDrag;

            ctx.ChangeCounter++;
            if (ctx.ChangeCounter > CruiserDirector.CruiseRollStart)
            {
                //原代码是 rand.NextBool(150) || cc > 200,骰点在前。客户端不消耗随机数,只等包
                bool rolled = IsServer && Main.rand.NextBool(CruiserDirector.CruiseRollChance);
                if (rolled || ctx.ChangeCounter > CruiserDirector.CruiseHardEnd)
                {
                    return NextAttack(ctx);
                }
            }
            return null;
        }
    }
}
