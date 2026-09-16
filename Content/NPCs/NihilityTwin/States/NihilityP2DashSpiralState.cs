using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 二阶段 2:螺旋冲刺。冲刺骨架与 0 号完全同构(同一套子计时、同样的 20 / -30 门槛),
    /// 区别只有三处:只冲 3 次、细胞改吐每 6 帧一圈五发的<b>慢速</b>螺旋墙(基准角随全局帧 ×19 度推进)、
    /// 本体侧刺改成每 40 帧一对且不带位置抖动。
    /// <para>
    /// <b>与原代码的一处差异</b>:那对侧刺在原代码里漏在 <c>!client</c> 守卫<b>外面</b>,
    /// 于是每个客户端都会各自造一发不同步的幽灵弹幕。这里统一走带守卫的出手口。
    /// 单机行为逐帧不变,只修掉联机侧的重复生成
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P2DashSpiral, typeof(NihilityStateContext))]
    public class NihilityP2DashSpiralState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P2DashSpiral;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            if (ctx.Num1 > NihilityDirector.SpiralReps) {
                ctx.Num2 = 0f;
                return EndAttack(ctx);
            }

            ctx.Num2--;
            if (ctx.Num2 < NihilityDirector.DashSubTimerFloor) {
                ctx.Num1++;
                ctx.Num2 = NihilityDirector.DashSubTimerReset;
                if (ctx.Num1 <= NihilityDirector.SpiralReps) {
                    CEUtils.PlaySound("beast_ghostdash" + Main.rand.Next(1, 5), 1, npc.Center);
                }
            }
            if (ctx.Num2 > 0f) {
                npc.velocity += npc.rotation.ToRotationVector2() * NihilityDirector.DashThrust;
                TrailBurst(ctx);
            }
            else {
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, (targetPos - npc.Center).ToRotation(), NihilityDirector.DashTurnRate, false);
            }
            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.DashCellThrust;

            if (IsServer) {
                if (ctx.FrameCounter % NihilityDirector.SpiralRingInterval == 0) {
                    float rot = MathHelper.ToRadians((Main.GameUpdateCount * NihilityDirector.SpiralAngleScale) % 360);
                    for (int i = 0; i < 360; i += NihilityDirector.SpiralRingStepDeg) {
                        Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                            (rot + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.SpiralRingSpeed,
                            BulletDamage(ctx), NihilityDirector.BulletKnockback);
                    }
                }
                if (ctx.FrameCounter % NihilityDirector.SpiralSpikeInterval == 0) {
                    Shoot<CellSpike>(npc.GetSource_FromThis(), npc.Center,
                        (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.SpiralSpikeSpeed,
                        BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                    Shoot<CellSpike>(npc.GetSource_FromThis(), npc.Center,
                        (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.SpiralSpikeSpeed,
                        BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                }
            }

            npc.velocity *= NihilityDirector.DashDrag;
            return null;
        }
    }
}
