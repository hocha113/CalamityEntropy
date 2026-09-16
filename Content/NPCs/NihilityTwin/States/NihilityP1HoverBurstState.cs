using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 一阶段 3:悬停爆发。先抢占玩家正上方 200 的位置,进入 800 内后转为持续推进,
    /// 细胞被外推到本体前方 200,每 30 帧吐一圈六发。
    /// <para>
    /// 接敌段自带一道刹车:如果按当前速度再走两帧就会进到 800 以内,速度先 ×0.36,
    /// 免得一头扎过头。这是原代码里唯一一处"预判自己会不会冲过站"的写法。
    /// </para>
    /// <para>
    /// 收招判定写在推进段<b>之前</b>,而收招会把计时清零,所以收招那一帧推进段整段不执行,
    /// 只有末尾那句朝向照常跑。照搬这个顺序
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P1HoverBurst, typeof(NihilityStateContext))]
    public class NihilityP1HoverBurstState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P1HoverBurst;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            TrailBurst(ctx);

            if (ctx.Num1 > 0 || CEUtils.getDistance(targetPos, npc.Center) < NihilityDirector.HoverApproachDistance)
            {
                ctx.Num1++;
            }
            else
            {
                if (npc.velocity.Length() > NihilityDirector.HoverApproachSpeedCap)
                {
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * NihilityDirector.HoverApproachSpeedCap;
                }
                npc.velocity = (targetPos - new Vector2(0, NihilityDirector.HoverApproachHeight) - npc.Center) * NihilityDirector.HoverApproachFollow;
                if (CEUtils.getDistance(targetPos, npc.Center + npc.velocity * 2) < NihilityDirector.HoverApproachDistance)
                {
                    npc.velocity *= NihilityDirector.HoverApproachBrake;
                }
            }

            IVaultState<NihilityStateContext> next = null;
            if (ctx.Num1 > NihilityDirector.HoverDuration)
            {
                next = EndAttack(ctx);
            }

            if (ctx.Num1 > 0)
            {
                npc.velocity *= NihilityDirector.HoverDrag;
                npc.velocity += (targetPos - npc.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.HoverThrust;
                cell.velocity = (npc.Center + (targetPos - npc.Center).SafeNormalize(Vector2.UnitX) * NihilityDirector.HoverCellReach - cell.Center) * NihilityDirector.HoverCellLerp;
                if (IsServer && ctx.FrameCounter % NihilityDirector.HoverRingInterval == 0)
                {
                    float rot = CEUtils.randomRot();
                    for (int i = 0; i < 360; i += NihilityDirector.HoverRingStepDeg)
                    {
                        Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                            (rot + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.HoverRingSpeed,
                            BulletDamage(ctx), NihilityDirector.BulletKnockback);
                    }
                }
            }

            npc.rotation = npc.velocity.ToRotation();
            return next;
        }
    }
}
