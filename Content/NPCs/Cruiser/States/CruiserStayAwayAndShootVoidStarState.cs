using CalamityEntropy.Content.NPCs.Cruiser.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 拉开:先沿当前航向散开(低于 30 每帧 ×1.1,否则 ×0.97),
    /// 70 帧起把速度往玩家方向掰,120 帧起加推力硬逼近,140 帧收招。
    /// 第 90 帧发一次尾鞭请求,那是尾部新星在全局唯一的两个触发口之一
    /// </summary>
    [VaultState((int)CruiserStateIndex.StayAwayAndShootVoidStar, typeof(CruiserStateContext))]
    public class CruiserStayAwayAndShootVoidStarState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.StayAwayAndShootVoidStar;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            Vector2 dir = (player.Center - npc.Center).normalize();

            if (npc.velocity.Length() < CruiserDirector.StayAwaySpeedCap)
            {
                npc.velocity *= CruiserDirector.StayAwayAccel;
            }
            else
            {
                npc.velocity *= CruiserDirector.StayAwayDrag;
            }

            ctx.ChangeCounter++;
            if (ctx.ChangeCounter == CruiserDirector.StayAwayWhipCue)
            {
                ctx.TailWhipCue = true;
                MarkNetUpdate(ctx);
            }
            if (ctx.ChangeCounter > CruiserDirector.StayAwayTurnStart)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, dir * npc.velocity.Length(), CruiserDirector.StayAwayTurnLerp);
                if (npc.velocity.Length() < CruiserDirector.StayAwaySpeedCap)
                {
                    npc.velocity *= CruiserDirector.StayAwayTurnAccel;
                }
            }
            if (ctx.ChangeCounter > CruiserDirector.StayAwayPushStart)
            {
                if (npc.velocity.Length() < CruiserDirector.StayAwaySpeedCap)
                {
                    npc.velocity *= CruiserDirector.StayAwayPushAccel;
                }
                npc.velocity += dir * CruiserDirector.StayAwayPushThrust;
                npc.velocity = Vector2.Lerp(npc.velocity, dir * npc.velocity.Length(), CruiserDirector.StayAwayPushLerp);
                npc.velocity *= CruiserDirector.StayAwayPushDrag;
            }
            if (ctx.ChangeCounter > CruiserDirector.StayAwayDuration)
            {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
