using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 二阶段 3:对撞合体。前 40 帧两端互相反推拉开距离(绳索同时被拉回满显示),
    /// 之后以 3.4/帧 的加速度对撞;一旦下一帧就会穿过去,当场对齐到中点、爆出 36 发环射并收招。
    /// <para>
    /// 收招之后还有一枚 1/2 的硬币:掷中就原地再来一次(跳过整备)。这是原代码自带的复读阀,
    /// <b>原样保留</b>,只是把掷点收归权威端——结果靠状态号过线。
    /// </para>
    /// <para>命中那一帧两端的位置是<b>直写</b>的,所以要顺手丢掉客户端纠偏器的旧预测,免得被当成失步</para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P2Merge, typeof(NihilityStateContext))]
    public class NihilityP2MergeState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P2Merge;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;

            if (ctx.Num1 == 1) {
                CEUtils.PlaySound("beast_lavaball_rise1", 1);
            }
            npc.rotation = (cell.Center - npc.Center).ToRotation() + MathHelper.Pi;
            ctx.Owner.ropeLerp = 1;
            ctx.Num1++;

            if (ctx.Num1 > NihilityDirector.MergeWindupFrames) {
                npc.velocity += (cell.Center - npc.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.MergeClosingForce;
                cell.velocity += (npc.Center - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.MergeClosingForce;
                if (CEUtils.getDistance(npc.Center, cell.Center) < npc.velocity.Length() + cell.velocity.Length() + NihilityDirector.MergeContactPadding) {
                    Vector2 midPos = (npc.Center + cell.Center) / 2;
                    npc.velocity *= 0;
                    cell.velocity *= 0;
                    ctx.Owner.TeleportBody(midPos + npc.rotation.ToRotationVector2() * NihilityDirector.MergeSplitOffset);
                    ctx.Owner.PlaceCell(midPos - npc.rotation.ToRotationVector2() * NihilityDirector.MergeSplitOffset);
                    if (IsServer) {
                        float rot = MathHelper.ToRadians((Main.GameUpdateCount * NihilityDirector.MergeAngleScale) % 360);
                        for (int i = 0; i < 360; i += NihilityDirector.MergeBurstStepDeg) {
                            Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                                (rot + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.MergeBurstSpeed,
                                BulletDamage(ctx), NihilityDirector.BulletKnockback);
                        }
                    }
                    CEUtils.PlaySound("flashback", 1, npc.Center);

                    IVaultState<NihilityStateContext> next = EndAttack(ctx);
                    if (IsServer && Main.rand.NextBool(NihilityDirector.MergeRepeatChance)) {
                        next = NihilityRotation.Create(NihilityStateIndex.P2Merge);
                    }
                    return next;
                }
            }
            else {
                npc.velocity += (npc.Center - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.MergeSpreadForce;
                cell.velocity -= (npc.Center - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.MergeSpreadForce;
            }
            return null;
        }
    }
}
