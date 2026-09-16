using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 一阶段 6:绕细胞盘旋。细胞自己扑向玩家并被点亮(每帧把发光续期写回细胞的 <c>ai[2]</c>),
    /// 本体以恒定 18 的速度、0.07 的转向率绕着细胞飞,于是两者拉出一个不断收紧的圆。
    /// <para>
    /// 环射的基准角在第 1 帧随机抽一次,之后每帧自转 0.5°。前 160 帧是每 10 帧一圈五发的稳定弹幕,
    /// 之后转为逐帧按 1/3 概率、位置抖动 ±44、速度 26 的高速散射——同一条环在后半段变成弹雨。
    /// </para>
    /// <para>
    /// 基准角原本在各端各抽各的(原代码这一行没有 netMode 门),靠 <c>ai[2]</c> 同步槽覆盖回来;
    /// 新骨架里它是 <see cref="NihilityStateContext.Num2"/>,掷点收归权威端、结果随 ExtraAI 过线,
    /// 逐帧那 0.5° 的自转仍然各端都跑
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P1Circle, typeof(NihilityStateContext))]
    public class NihilityP1CircleState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P1Circle;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            cell.velocity *= NihilityDirector.CircleCellDrag;
            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.CircleCellThrust;
            cell.ai[2] = NihilityDirector.CircleCellGlow;
            npc.velocity = npc.rotation.ToRotationVector2() * NihilityDirector.CircleSpeed;
            npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, (cell.Center - npc.Center).ToRotation(), NihilityDirector.CircleTurnRate, false);

            if (ctx.Num1 == NihilityDirector.CircleRingRollFrame && IsServer)
            {
                ctx.Num2 = CEUtils.randomRot();
                MarkNetUpdate(ctx);
            }
            ctx.Num2 += MathHelper.ToRadians(NihilityDirector.CircleRingSpinDeg);
            ctx.Num1++;

            if (IsServer)
            {
                if (Main.GameUpdateCount % NihilityDirector.CircleSpikeInterval == 0)
                {
                    Shoot<CellSpike>(npc.GetSource_FromThis(), npc.Center,
                        (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.CircleSpikeSpeed,
                        BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                    Shoot<CellSpike>(npc.GetSource_FromThis(), npc.Center,
                        (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.CircleSpikeSpeed,
                        BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                }
                if (ctx.Num1 < NihilityDirector.CircleBurstFrame)
                {
                    if (Main.GameUpdateCount % NihilityDirector.CircleRingInterval == 0)
                    {
                        for (int i = 0; i < 360; i += NihilityDirector.CircleRingStepDeg)
                        {
                            Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                                (ctx.Num2 + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.CircleRingSpeed,
                                BulletDamage(ctx), NihilityDirector.BulletKnockback);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 360; i += NihilityDirector.CircleRingStepDeg)
                    {
                        if (Main.rand.NextBool(NihilityDirector.CircleBurstChance))
                        {
                            Shoot<CellBullet>(cell.GetSource_FromThis(),
                                cell.Center + new Vector2(Main.rand.Next(-NihilityDirector.CircleBurstScatter, NihilityDirector.CircleBurstScatter), Main.rand.Next(-NihilityDirector.CircleBurstScatter, NihilityDirector.CircleBurstScatter)),
                                (ctx.Num2 + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.CircleBurstSpeed,
                                BulletDamage(ctx), NihilityDirector.BulletKnockback);
                        }
                    }
                }
            }

            if (ctx.Num1 > NihilityDirector.CircleDuration)
            {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
