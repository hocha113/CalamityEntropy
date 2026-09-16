using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 二阶段 0:冲刺齐射。六次"转向 → 猛推"的锁向冲刺,细胞全程扑玩家并每 30 帧吐一圈四发,
    /// 本体每 10 帧补一对带抖动的侧刺。
    /// <para>
    /// 节拍全靠子计时 <see cref="NihilityStateContext.Num2"/>:每帧 -1,为正是推进窗(沿朝向 +5/帧),
    /// 为负是转向窗(0.09 追瞄);跌破 -30 就重置成 20 并记一次冲刺,所以一次冲刺周期固定 50 帧。
    /// <c>Num1</c> 在这一手里是<b>冲刺次数</b>而不是帧计时。
    /// </para>
    /// <para>
    /// 子计时进状态时<b>不清零</b>(原代码的 <c>ai[2]</c> 是跨招共用槽),所以第一次冲刺的相位
    /// 取决于上一手在这个槽里留了什么残值。照搬
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P2DashVolley, typeof(NihilityStateContext))]
    public class NihilityP2DashVolleyState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P2DashVolley;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            if (ctx.Num1 > NihilityDirector.DashVolleyReps)
            {
                ctx.Num2 = 0f;
                return EndAttack(ctx);
            }

            ctx.Num2--;
            if (ctx.Num2 < NihilityDirector.DashSubTimerFloor)
            {
                ctx.Num1++;
                ctx.Num2 = NihilityDirector.DashSubTimerReset;
                if (ctx.Num1 <= NihilityDirector.DashVolleyReps)
                {
                    //音效选号吃随机数,各端各选各的;不参与任何判定,照搬原位置
                    CEUtils.PlaySound("beast_ghostdash" + Main.rand.Next(1, 5), 1, npc.Center);
                }
            }
            if (ctx.Num2 > 0f)
            {
                npc.velocity += npc.rotation.ToRotationVector2() * NihilityDirector.DashThrust;
                TrailBurst(ctx);
            }
            else
            {
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, (targetPos - npc.Center).ToRotation(), NihilityDirector.DashTurnRate, false);
            }
            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.DashCellThrust;

            if (IsServer)
            {
                if (ctx.FrameCounter % NihilityDirector.DashRingInterval == 0)
                {
                    float rot = CEUtils.randomRot();
                    for (int i = 0; i < 360; i += NihilityDirector.DashRingStepDeg)
                    {
                        Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                            (rot + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.DashRingSpeed,
                            BulletDamage(ctx), NihilityDirector.BulletKnockback);
                    }
                }
                if (ctx.FrameCounter % NihilityDirector.DashSpikeInterval == 0)
                {
                    Shoot<CellSpike>(npc.GetSource_FromThis(),
                        npc.Center + new Vector2(Main.rand.NextFloat(-NihilityDirector.DashSpikeScatter, NihilityDirector.DashSpikeScatter), Main.rand.NextFloat(-NihilityDirector.DashSpikeScatter, NihilityDirector.DashSpikeScatter)),
                        (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.DashSpikeSpeed,
                        BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                    Shoot<CellSpike>(npc.GetSource_FromThis(),
                        npc.Center + new Vector2(Main.rand.NextFloat(-NihilityDirector.DashSpikeScatter, NihilityDirector.DashSpikeScatter), Main.rand.NextFloat(-NihilityDirector.DashSpikeScatter, NihilityDirector.DashSpikeScatter)),
                        (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.DashSpikeSpeed,
                        BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                }
            }

            npc.velocity *= NihilityDirector.DashDrag;
            return null;
        }
    }
}
