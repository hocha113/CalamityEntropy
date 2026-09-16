using CalamityEntropy.Content.NPCs.Apsychos.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 接近:转向玩家,推力按距离重映射。计时每帧 +2(贴脸再 +1),所以本段时长会被贴近压缩。
    /// 对应原 AIChangeCounter 在块内自增一次、贴脸再一次、块外再一次
    /// </summary>
    [VaultState((int)ApsychosStateIndex.MoveToTarget, typeof(ApsychosStateContext))]
    public class ApsychosMoveToTargetState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.MoveToTarget;

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;

            ctx.TailStyle = ApsychosTailStyle.Follow;

            //块内第一次自增。基类在 OnUpdate 返回后再自增一次,合起来每帧 +2
            Timer++;
            float targetRot = (player.Center - npc.Center).ToRotation();
            npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.ApproachRotateFixed, true);
            npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.ApproachRotateLerp, false);
            npc.velocity *= ApsychosDirector.ApproachDrag;
            float spd = Utils.Remap(ctx.TargetDistance,
                ApsychosDirector.ApproachNearDistance, ApsychosDirector.ApproachFarDistance,
                ApsychosDirector.ApproachThrustNear, ApsychosDirector.ApproachThrustFar);
            npc.velocity += npc.rotation.ToRotationVector2() * spd * enrange;
            if (ctx.TargetDistance < ApsychosDirector.ApproachCloseDistance) {
                Timer++;
            }
            if (Timer > ApsychosDirector.ApproachDurationBase / enrange) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
