using CalamityEntropy.Content.NPCs.Apsychos.Core;
using CalamityEntropy.Content.Projectiles.ApsychosProjs;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 甩尾:num1 是本次鞭击的帧计时(到 60 发射),num2 是尾巴伸出的速度累加器,num3 是伸出距离。
    /// 鞭击次数走 <see cref="ApsychosStateContext.TailDashReps"/>,收招前自己清零(原代码也不在 SetAIStyle 里清)。
    /// 发射那一帧把当时的朝向烙进速度,所以朝向必须过线,否则联机这一脚会打歪
    /// </summary>
    [VaultState((int)ApsychosStateIndex.TailDash, typeof(ApsychosStateContext))]
    public class ApsychosTailDashState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.TailDash;

        /// <summary>本地发射锁存,不过线。慢半拍的客户端在宽限窗内仍把朝向烙进速度</summary>
        private bool launched;

        public override void OnEnter(ApsychosStateContext ctx) {
            base.OnEnter(ctx);
            launched = false;
        }

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC tail = ctx.Tail;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;

            ctx.TailStyle = ApsychosTailStyle.OnePoint;
            ctx.Num1++;
            if (ctx.Num1 < ApsychosDirector.TailDashWindupFrames) {
                launched = false;
                float targetRot = (player.Center - npc.Center).ToRotation();
                if (ctx.Num1 > ApsychosDirector.TailDashRetractStartFrame) {
                    ctx.Num3 = float.Lerp(ctx.Num3, ApsychosDirector.TailDashRetractTarget, ApsychosDirector.TailDashRetractLerp);
                }
                tail.Center = npc.Center + npc.rotation.ToRotationVector2() * ctx.Num3;
                if (ctx.Num1 > ApsychosDirector.TailDashAimStartFrame) {
                    npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.TailDashRotateFixed, true);
                    npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.TailDashRotateLerp, false);
                }
                npc.velocity *= ApsychosDirector.TailDashWindupDrag;
                float spd = Utils.Remap(ctx.TargetDistance,
                    ApsychosDirector.ApproachNearDistance, ApsychosDirector.ApproachFarDistance,
                    ApsychosDirector.TailDashThrustNear, ApsychosDirector.TailDashThrustFar);
                npc.velocity += npc.rotation.ToRotationVector2() * spd * enrange;
            }
            if (ctx.Num1 >= ApsychosDirector.TailDashWindupFrames && !launched) {
                launched = true;
                if (!CuePassed(ctx.Num1, ApsychosDirector.TailDashWindupFrames)) {
                    npc.velocity = npc.rotation.ToRotationVector2() * ApsychosDirector.TailDashLaunchSpeed * enrange;
                    MarkNetUpdate(ctx);
                }
            }
            if (ctx.Num1 >= ApsychosDirector.TailDashWindupFrames) {
                npc.velocity *= ApsychosDirector.TailDashLungeDrag;
                ctx.Num2 *= ApsychosDirector.TailDashExtendDamp;
                ctx.Num2 += ApsychosDirector.TailDashExtendAccel;
                ctx.Num3 += ctx.Num2;
                tail.Center = npc.Center + npc.rotation.ToRotationVector2() * ctx.Num3;
                if (ctx.Num3 > ApsychosDirector.TailDashStrikeDistance) {
                    CEUtils.PlaySound("scatter", 1.6f, npc.Center, volume: 0.9f);
                    Shoot<ApsychosTailShoot>(ctx, tail.Center, tail.rotation.ToRotationVector2() * ApsychosDirector.TailDashProjSpeed * enrange,
                        ApsychosDirector.TailDashDamageMult, npc.scale);
                    ctx.TailDashReps++;
                    ctx.Num1 = 0f;
                    ctx.Num2 = 0f;
                    MarkNetUpdate(ctx);
                }
            }
            if (ctx.TailDashReps > ApsychosDirector.TailDashReps(npc)) {
                ctx.TailDashReps = 0;
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
