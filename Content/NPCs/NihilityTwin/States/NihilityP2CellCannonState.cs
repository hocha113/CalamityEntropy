using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 二阶段 1:细胞炮。细胞先被吸回本体 90 以内,随后被<b>焊死</b>在本体后方 100(逐帧改写位置、速度清零),
    /// 本体带着它自旋蓄力;计时走到 100 的那一帧锁向发射,细胞以 60 的速度飞出去并沿途双侧散射;
    /// 130 帧后自旋停,150 帧后细胞恢复自由并重新扑向玩家,160 帧收招。
    /// <para>
    /// 蓄力期每两帧额外 +1 计时,所以 100 帧的蓄力窗实际只占约 67 帧真实时间。
    /// 也正因为一帧可能跳两格,发射拍必须落在「窗口条件 &lt; 100 与等值判定 == 100」这对互补判定上:
    /// 计时从 99 起跳时窗口先失效,双跳不会发生,所以 100 恒被命中一次。
    /// </para>
    /// <para>
    /// 发射帧把当时的朝向烙进 <see cref="NihilityStateContext.Num3"/>(原代码的 <c>NPC.ai[3]</c>),
    /// 之后 30 帧细胞按它反向定速。<c>ai[3]</c> 在新骨架里归状态号占用,所以它挪成过线字段
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P2CellCannon, typeof(NihilityStateContext))]
    public class NihilityP2CellCannonState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P2CellCannon;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            ctx.KeepRotSpeed = true;

            if (ctx.Num1 > 0 || CEUtils.getDistance(cell.Center, npc.Center) < NihilityDirector.CannonDockDistance)
            {
                ctx.Num1++;
            }
            else
            {
                npc.velocity *= NihilityDirector.CannonDockDrag;
                cell.velocity += (npc.Center - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.CannonDockPull;
            }

            if (ctx.Num1 > 0 && ctx.Num1 < NihilityDirector.CannonChargeFrames)
            {
                npc.velocity *= NihilityDirector.CannonDrag;
                npc.velocity = (targetPos - npc.Center) * NihilityDirector.CannonFollow;
                npc.rotation += ctx.RotSpeed;
                ctx.RotSpeed += NihilityDirector.CannonRotAccel;
                ctx.RotSpeed *= NihilityDirector.CannonRotDamp;
                ctx.Owner.PlaceCell(npc.Center + new Vector2(NihilityDirector.CannonCellOffset, 0).RotatedBy(npc.rotation));
                cell.velocity *= 0;
                if (ctx.FrameCounter % NihilityDirector.CannonDoubleTickInterval == 0)
                {
                    ctx.Num1++;
                }
            }

            if (ctx.Num1 == NihilityDirector.CannonChargeFrames)
            {
                npc.rotation = (targetPos - npc.Center).ToRotation();
                cell.velocity = npc.rotation.ToRotationVector2() * NihilityDirector.CannonLaunchSpeed;
                npc.rotation += MathHelper.Pi;
                ctx.Owner.PlaceCell(npc.Center + new Vector2(NihilityDirector.CannonCellOffset, 0).RotatedBy(npc.rotation));
                ctx.Num3 = npc.rotation;
                MarkNetUpdate(ctx);
            }

            if (ctx.Num1 > NihilityDirector.CannonChargeFrames && ctx.Num1 < NihilityDirector.CannonRecoilEndFrame)
            {
                ctx.RotSpeed *= NihilityDirector.CannonRecoilRotDamp;
                npc.rotation += ctx.RotSpeed;
                cell.velocity = ctx.Num3.ToRotationVector2() * NihilityDirector.CannonTrailSpeed;
                if (ctx.FrameCounter % NihilityDirector.CannonFireInterval == 0 && IsServer)
                {
                    Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                        (cell.velocity.ToRotation() + MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.CannonBulletSpeed,
                        BulletDamage(ctx), NihilityDirector.BulletKnockback);
                    Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                        (cell.velocity.ToRotation() - MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.CannonBulletSpeed,
                        BulletDamage(ctx), NihilityDirector.BulletKnockback);
                }
            }

            if (ctx.Num1 > NihilityDirector.CannonRecallFrame)
            {
                cell.velocity *= NihilityDirector.CannonCellDrag;
                cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.CannonCellThrust;
            }

            if (ctx.Num1 > NihilityDirector.CannonDuration)
            {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
