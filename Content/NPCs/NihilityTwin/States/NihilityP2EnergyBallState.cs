using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 二阶段 6:能量球。第 2、62、122 帧各在细胞身上挂九颗定向能量球(初速为零,方向写进 ai1,
    /// 由弹幕自己延时起飞),其余时间两端都缓慢贴向玩家。
    /// <para>
    /// 三个拍点都是「等值判定」而计时每帧只 +1,所以不会漏拍;它们只在权威端生成弹幕,
    /// 基准角在权威端抽取,九颗的相对角由 <c>ai1</c> 带给每一颗自己
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P2EnergyBall, typeof(NihilityStateContext))]
    public class NihilityP2EnergyBallState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P2EnergyBall;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            if ((ctx.Num1 == NihilityDirector.BallCueFrame1 || ctx.Num1 == NihilityDirector.BallCueFrame2 || ctx.Num1 == NihilityDirector.BallCueFrame3) && IsServer) {
                float rot = CEUtils.randomRot();
                for (int i = 0; i < 360; i += NihilityDirector.BallStepDeg) {
                    Shoot<NihilityEnergyBall>(cell.GetSource_FromThis(), cell.Center, Vector2.Zero,
                        BulletDamage(ctx), NihilityDirector.BulletKnockback,
                        cell.whoAmI, rot + MathHelper.ToRadians(i));
                }
            }

            npc.velocity *= NihilityDirector.BallDrag;
            ctx.Num1++;
            npc.velocity += (targetPos - npc.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.BallThrust;
            npc.rotation = npc.velocity.ToRotation();
            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.UnitX) * NihilityDirector.BallCellThrust;

            if (ctx.Num1 > NihilityDirector.BallDuration) {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
