using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 虚空残渣:张嘴蓄 80 帧(距离大于 1000 时快速补位),第 80 帧一口喷出 80 发残渣,
    /// 80~100 帧合嘴,140 帧起顺着朝向冲一段,200 帧收招。
    /// 嘴部开合是纯绘制量,判据用的是自增<b>前</b>的计数(原代码就写在自增之前)
    /// </summary>
    [VaultState((int)CruiserStateIndex.VoidResidue, typeof(CruiserStateContext))]
    public class CruiserVoidResidueState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.VoidResidue;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            Vector2 dir = (player.Center - npc.Center).normalize();

            //自增前:嘴部开合
            if (ctx.ChangeCounter < CruiserDirector.ResidueMouthOpenUntil)
            {
                ctx.MouthRot += CruiserDirector.ResidueMouthOpenRate;
            }
            else if (ctx.ChangeCounter < CruiserDirector.ResidueMouthCloseUntil)
            {
                ctx.MouthRot += CruiserDirector.ResidueMouthCloseRate;
            }

            ctx.ChangeCounter++;

            if (ctx.ChangeCounter < CruiserDirector.ResidueBurstFrame
                && npc.Distance(player.Center) > CruiserDirector.ResidueApproachDistance)
            {
                npc.velocity *= CruiserDirector.ResidueFarDrag;
                npc.velocity += dir * CruiserDirector.ResidueFarThrust;
            }
            else
            {
                npc.velocity *= CruiserDirector.ResidueNearDrag;
                npc.velocity += dir * CruiserDirector.ResidueNearThrust;
            }
            if (ctx.ChangeCounter == CruiserDirector.ResidueSoundFrame)
            {
                CEUtils.PlaySound("voidSound", CruiserDirector.ResidueSoundPitch, npc.Center);
            }
            if (ctx.ChangeCounter == CruiserDirector.ResidueBurstFrame)
            {
                if (IsServer)
                {
                    for (int i = 0; i < CruiserDirector.ResidueBurstCount; i++)
                    {
                        Shoot(ctx, ModContent.ProjectileType<VoidResidue>(), npc.Center,
                            npc.velocity.normalize().RotatedByRandom(CruiserDirector.ResidueBurstSpread)
                                * CruiserDirector.ResidueBurstSpeed
                                * Main.rand.NextFloat(CruiserDirector.ResidueBurstSpeedMin, CruiserDirector.ResidueBurstSpeedMax),
                            CruiserDirector.ResidueDamageMult);
                    }
                    MarkNetUpdate(ctx);
                }
                CEUtils.PlaySound("CruiserSpit2", 1.4f, npc.Center);
                CEUtils.PlaySound("CruiserVoidResidue", 1, npc.Center);
            }
            if (ctx.ChangeCounter > CruiserDirector.ResidueLungeStart)
            {
                npc.velocity += npc.rotation.ToRotationVector2() * CruiserDirector.ResidueLungeThrust;
                npc.velocity *= CruiserDirector.ResidueLungeDrag;
            }
            if (ctx.ChangeCounter > CruiserDirector.ResidueDuration)
            {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
