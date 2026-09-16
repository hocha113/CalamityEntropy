using CalamityEntropy.Content.NPCs.Cruiser.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 直扑:一路加速撞向玩家。推力、阻尼、转向插值三项都按当前距离重映射,
    /// 越近推得越轻、掰得越弱,所以贴到脸上时是靠惯性掠过而不是原地绕。
    /// 收招:计时过 600,或距离小于 700 + 当前速度(速度越快越早交棒)
    /// </summary>
    [VaultState((int)CruiserStateIndex.TryToClosePlayer, typeof(CruiserStateContext))]
    public class CruiserTryToClosePlayerState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.TryToClosePlayer;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float dist = npc.Distance(player.Center);
            Vector2 dir = (player.Center - npc.Center).normalize();

            if (npc.velocity.Length() < CruiserDirector.CloseInSpeedCap) {
                npc.velocity *= CruiserDirector.CloseInAccel;
            }
            npc.velocity += dir * Utils.Remap(dist, CruiserDirector.CloseInRemapNear, CruiserDirector.CloseInRemapFar,
                CruiserDirector.CloseInThrustNear, CruiserDirector.CloseInThrustFar);
            npc.velocity *= Utils.Remap(dist, CruiserDirector.CloseInRemapNear, CruiserDirector.CloseInRemapFar,
                CruiserDirector.CloseInDragNear, CruiserDirector.CloseInDragFar);
            npc.velocity = Vector2.Lerp(npc.velocity, dir * npc.velocity.Length(),
                Utils.Remap(dist, CruiserDirector.CloseInRemapNear, CruiserDirector.CloseInAimRemapFar,
                    CruiserDirector.CloseInAimLerpNear, CruiserDirector.CloseInAimLerpFar));

            ctx.ChangeCounter++;
            if (ctx.ChangeCounter > CruiserDirector.CloseInDuration
                || dist < CruiserDirector.CloseInHandoffDistance + npc.velocity.Length()) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
