using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 二阶段 5:口部激光。前 40 帧本体把口部转到<b>背离</b>玩家的方向蓄力(定速 4°/帧),
    /// 第 40 帧开出一条挂在本体口部、存活 400 帧的扫射光束,之后全程只用 0.01 的比例追瞄慢慢扫,
    /// 160 帧后再叠一档 1.4°/帧 的定速追瞄。细胞同时以 1/3 的概率贴脸散射。
    /// <para>
    /// 这一手是整场唯一切换本体贴图的招(绘制层按「阶段 2 + 本状态」取 BodyAlt),
    /// 所以绘制要读得到状态号。
    /// </para>
    /// <para>光束自己会跟着本体的 <c>Center</c> 与 <c>rotation</c> 走,所以本体朝向必须过线</para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P2Laser, typeof(NihilityStateContext))]
    public class NihilityP2LaserState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P2Laser;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            if (ctx.Num1 == NihilityDirector.LaserCueFrame)
            {
                CEUtils.PlaySound("charge", 1, npc.Center);
                CEUtils.PlaySound("charge", 1, npc.Center);
            }
            if (ctx.Num1 < NihilityDirector.LaserAimFrames)
            {
                npc.velocity = (targetPos - npc.Center) * NihilityDirector.LaserAimFollow;
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, (npc.Center - targetPos).ToRotation(), NihilityDirector.LaserAimRateDeg.ToRadians(), true);
            }
            else
            {
                if (ctx.Num1 > NihilityDirector.LaserFastTrackFrame)
                {
                    npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, (targetPos - npc.Center).ToRotation(), NihilityDirector.LaserFastTrackDeg.ToRadians(), true);
                }
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, (targetPos - npc.Center).ToRotation(), NihilityDirector.LaserSlowTrackRate, false);
            }

            if (ctx.Num1 == NihilityDirector.LaserAimFrames)
            {
                Shoot<CruiserLaserMouth>(npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
                    (int)(npc.damage / NihilityDirector.LaserDamageDivisor), NihilityDirector.LaserKnockback,
                    npc.whoAmI, NihilityDirector.LaserBeamLifetime);
            }

            if (IsServer && Main.rand.NextBool(NihilityDirector.LaserSprayChance))
            {
                Shoot<CellBullet>(cell.GetSource_FromThis(),
                    cell.Center + new Vector2(Main.rand.NextFloat(-NihilityDirector.LaserSprayScatter, NihilityDirector.LaserSprayScatter), Main.rand.NextFloat(-NihilityDirector.LaserSprayScatter, NihilityDirector.LaserSprayScatter)),
                    (cell.Center - targetPos).SafeNormalize(Vector2.UnitX) * NihilityDirector.LaserSpraySpeed,
                    BulletDamage(ctx), NihilityDirector.BulletKnockback);
            }

            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.LaserCellThrust;
            npc.velocity = (targetPos - npc.Center) * NihilityDirector.LaserFollow;
            ctx.Num1++;
            if (ctx.Num1 > NihilityDirector.LaserDuration)
            {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
