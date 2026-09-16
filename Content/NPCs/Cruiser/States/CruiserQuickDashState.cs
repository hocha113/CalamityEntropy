using CalamityEntropy.Content.NPCs.Cruiser.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 短冲:开局那一帧把朝向锁死到玩家身上(之后不再追瞄,预告指哪打哪),
    /// 沿锁定方向连推 38 帧,之后转为追瞄,100 帧收招。
    /// 锁向是决策点,在权威端打一次 netUpdate
    /// </summary>
    [VaultState((int)CruiserStateIndex.QuickDash, typeof(CruiserStateContext))]
    public class CruiserQuickDashState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.QuickDash;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            if (ctx.ChangeCounter == 0) {
                npc.rotation = (player.Center - npc.Center).ToRotation();
                MarkNetUpdate(ctx);
            }
            ctx.ChangeCounter++;

            if (ctx.ChangeCounter > CruiserDirector.QuickDashThrustFrames) {
                npc.velocity *= CruiserDirector.QuickDashChaseDrag;
                npc.velocity += (player.Center - npc.Center).normalize() * CruiserDirector.QuickDashChaseThrust;
            }
            else {
                npc.velocity += npc.rotation.ToRotationVector2() * CruiserDirector.QuickDashThrust;
            }
            if (ctx.ChangeCounter > CruiserDirector.QuickDashDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
