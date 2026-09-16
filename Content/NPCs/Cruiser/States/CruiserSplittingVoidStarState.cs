using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 裂空吐星:结构与虚空残渣同型,只是蓄力拉长到 100 帧、贴近阈值收到 900、
    /// 收招提前到 140 帧,且第 100 帧喷的是虚空星。第 20 帧有一声蓄力音当预告
    /// </summary>
    [VaultState((int)CruiserStateIndex.SplittingVoidStar, typeof(CruiserStateContext))]
    public class CruiserSplittingVoidStarState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.SplittingVoidStar;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            Vector2 dir = (player.Center - npc.Center).normalize();

            //自增前:嘴部开合与蓄力音
            if (ctx.ChangeCounter < CruiserDirector.SplitMouthOpenUntil) {
                ctx.MouthRot += CruiserDirector.SplitMouthOpenRate;
            }
            else if (ctx.ChangeCounter < CruiserDirector.SplitMouthCloseUntil) {
                ctx.MouthRot += CruiserDirector.SplitMouthCloseRate;
            }
            if (ctx.ChangeCounter == CruiserDirector.SplitSoundFrame) {
                CEUtils.PlaySound("voidSound", CruiserDirector.SplitSoundPitch, npc.Center);
            }

            ctx.ChangeCounter++;

            if (ctx.ChangeCounter < CruiserDirector.SplitBurstFrame
                && npc.Distance(player.Center) > CruiserDirector.SplitApproachDistance) {
                npc.velocity *= CruiserDirector.SplitFarDrag;
                npc.velocity += dir * CruiserDirector.SplitFarThrust;
            }
            else {
                npc.velocity *= CruiserDirector.SplitNearDrag;
                npc.velocity += dir * CruiserDirector.SplitNearThrust;
            }
            if (ctx.ChangeCounter == CruiserDirector.SplitBurstFrame) {
                if (IsServer) {
                    for (int i = 0; i < CruiserDirector.SplitBurstCount; i++) {
                        Shoot(ctx, ModContent.ProjectileType<VoidStar>(), npc.Center,
                            npc.velocity.normalize().RotatedByRandom(CruiserDirector.SplitBurstSpread)
                                * CruiserDirector.SplitBurstSpeed
                                * Main.rand.NextFloat(CruiserDirector.SplitBurstSpeedMin, CruiserDirector.SplitBurstSpeedMax),
                            CruiserDirector.SplitDamageMult);
                    }
                    MarkNetUpdate(ctx);
                }
                CEUtils.PlaySound("CruiserSpit", 1.2f, npc.Center);
                CEUtils.PlaySound("VoidBomb", 1.1f, npc.Center);
                CEUtils.PlaySound("VoidBomb", 1.1f, npc.Center);
                CEUtils.PlaySound("VoidBomb", 1.1f, npc.Center);
                CEUtils.PlaySound("vbuse", 1, npc.Center);
            }
            if (ctx.ChangeCounter > CruiserDirector.SplitDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
