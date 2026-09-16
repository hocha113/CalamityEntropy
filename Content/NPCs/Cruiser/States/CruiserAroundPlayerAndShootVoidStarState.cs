using CalamityEntropy.Content.NPCs.Cruiser.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 绕飞:锚点是「玩家看向本体的方向再旋转 0.6 弧度、外推 600」,所以本体会稳定地侧向绕圈。
    /// 每 40 帧甩一次尾鞭,共八次,350 帧收招。
    /// 尾部新星在这一手里会被削弱(环数 -2、每环减半、初速 ×0.45),削弱逻辑在链条落地那一侧
    /// </summary>
    [VaultState((int)CruiserStateIndex.AroundPlayerAndShootVoidStar, typeof(CruiserStateContext))]
    public class CruiserAroundPlayerAndShootVoidStarState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.AroundPlayerAndShootVoidStar;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            Vector2 targetPos = player.Center
                + (npc.Center - player.Center).normalize().RotatedBy(CruiserDirector.AroundOrbitAngle) * CruiserDirector.AroundOrbitRadius;
            npc.velocity += (targetPos - npc.Center).normalize() * CruiserDirector.AroundThrust;
            npc.velocity *= CruiserDirector.AroundDrag;

            ctx.ChangeCounter++;
            if (ctx.ChangeCounter % CruiserDirector.AroundWhipInterval == 0) {
                ctx.TailWhipCue = true;
                MarkNetUpdate(ctx);
            }
            if (ctx.ChangeCounter > CruiserDirector.AroundDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
