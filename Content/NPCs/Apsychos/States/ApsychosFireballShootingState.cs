using CalamityEntropy.Content.NPCs.Apsychos.Core;
using CalamityEntropy.Content.Projectiles.ApsychosProjs;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos.States
{
    /// <summary>
    /// 三连火球:尾巴前伸当炮口,齐射间隔除以 enrange。
    /// <c>num1 &lt;= 5</c> 才停,所以实际打 6 轮(原判定照搬)。本状态自管尾巴速度衰减
    /// </summary>
    [VaultState((int)ApsychosStateIndex.FireballShooting, typeof(ApsychosStateContext))]
    public class ApsychosFireballShootingState : ApsychosStateBase
    {
        public override ApsychosStateIndex StateIndex => ApsychosStateIndex.FireballShooting;

        public override IVaultState<ApsychosStateContext> OnUpdate(ApsychosStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC tail = ctx.Tail;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;

            ctx.DecayTailSpeed = false;
            npc.velocity *= ApsychosDirector.FireballDragA;
            float targetRot = (player.Center - npc.Center).ToRotation();
            npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, targetRot, ApsychosDirector.FireballRotate, false);
            npc.velocity *= ApsychosDirector.FireballDragB;
            npc.velocity += npc.rotation.ToRotationVector2() * ApsychosDirector.FireballThrust;
            ctx.TailStyle = ApsychosTailStyle.OnePoint;
            tail.Center = Vector2.Lerp(tail.Center, npc.Center + npc.rotation.ToRotationVector2() * ApsychosDirector.FireballTailReach * npc.scale, ApsychosDirector.FireballTailLerp * enrange);

            int total = ApsychosDirector.FireballVolleys;
            if (Timer > ApsychosDirector.FireballWindupBase / enrange)
            {
                if (ctx.Num2-- <= 0f)
                {
                    if (ctx.Num1 <= total)
                    {
                        ctx.Num2 = ApsychosDirector.FireballIntervalBase / enrange;
                        ctx.Num1++;
                        CEUtils.PlaySound("YharonFireball1", 0.9f, npc.Center);
                        CEUtils.PlaySound("YharonFireball1", 0.9f, npc.Center);
                        Vector2 muzzle = tail.Center + tail.rotation.ToRotationVector2() * ApsychosDirector.FireballMuzzleOffset * npc.scale;
                        Vector2 dir = tail.rotation.ToRotationVector2();
                        if (ctx.Phase == 1)
                        {
                            Shoot<ApsychosFireball>(ctx, muzzle, dir * ApsychosDirector.FireballSpeedCenterP1 * enrange, ApsychosDirector.FireballDamageMult, ctx.Phase);
                            Shoot<ApsychosFireball>(ctx, muzzle, dir.RotatedBy(ApsychosDirector.FireballSpreadP1) * ApsychosDirector.FireballSpeedSideP1 * enrange, ApsychosDirector.FireballDamageMult, ctx.Phase);
                            Shoot<ApsychosFireball>(ctx, muzzle, dir.RotatedBy(-ApsychosDirector.FireballSpreadP1) * ApsychosDirector.FireballSpeedSideP1 * enrange, ApsychosDirector.FireballDamageMult, ctx.Phase);
                        }
                        else
                        {
                            Shoot<ApsychosFireball>(ctx, muzzle, dir * ApsychosDirector.FireballSpeedP2 * enrange, ApsychosDirector.FireballDamageMult, ctx.Phase);
                            Shoot<ApsychosFireball>(ctx, muzzle, dir.RotatedBy(ApsychosDirector.FireballSpreadP2) * ApsychosDirector.FireballSpeedP2 * enrange, ApsychosDirector.FireballDamageMult, ctx.Phase);
                            Shoot<ApsychosFireball>(ctx, muzzle, dir.RotatedBy(-ApsychosDirector.FireballSpreadP2) * ApsychosDirector.FireballSpeedP2 * enrange, ApsychosDirector.FireballDamageMult, ctx.Phase);
                        }
                        //后坐是运动,各端都写;弹幕生成已在 Shoot 里守卫
                        tail.velocity = -tail.rotation.ToRotationVector2() * ApsychosDirector.FireballRecoil * enrange;
                        MarkNetUpdate(ctx);
                    }
                }
            }
            if (ctx.Num1 > total)
            {
                ctx.Num3++;
                if (ctx.Num3 > ApsychosDirector.FireballTailoffBase / enrange)
                {
                    return NextAttack(ctx);
                }
            }
            else
            {
                ctx.TailLight += ApsychosDirector.FireballTailLightRise;
            }
            tail.velocity *= ApsychosDirector.FireballTailDrag;
            return null;
        }
    }
}
