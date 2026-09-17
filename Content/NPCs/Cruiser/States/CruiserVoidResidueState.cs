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

        /// <summary>
        /// 两处一次性拍的锁存(本地,不过线)。原判据是 <c>ChangeCounter == 2</c> 与 <c>== 80</c>,
        /// ChangeCounter 带 ±2 容差收养,等值判定会被跨过——弹幕在权威端不受影响,
        /// 但音效各端本地放,漏掉就等于这一口喷射对客户端没有蓄力预告、也没有出手声
        /// </summary>
        private bool windupCued;
        private bool burstCued;

        public override void OnEnter(CruiserStateContext ctx) {
            base.OnEnter(ctx);
            windupCued = false;
            burstCued = false;
        }

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            Vector2 dir = (player.Center - npc.Center).normalize();

            //自增前:嘴部开合
            if (ctx.ChangeCounter < CruiserDirector.ResidueMouthOpenUntil) {
                ctx.MouthRot += CruiserDirector.ResidueMouthOpenRate;
            }
            else if (ctx.ChangeCounter < CruiserDirector.ResidueMouthCloseUntil) {
                ctx.MouthRot += CruiserDirector.ResidueMouthCloseRate;
            }

            ctx.ChangeCounter++;

            if (ctx.ChangeCounter < CruiserDirector.ResidueBurstFrame
                && npc.Distance(player.Center) > CruiserDirector.ResidueApproachDistance) {
                npc.velocity *= CruiserDirector.ResidueFarDrag;
                npc.velocity += dir * CruiserDirector.ResidueFarThrust;
            }
            else {
                npc.velocity *= CruiserDirector.ResidueNearDrag;
                npc.velocity += dir * CruiserDirector.ResidueNearThrust;
            }
            if (!windupCued && CuePassed(ctx.ChangeCounter, CruiserDirector.ResidueSoundFrame)) {
                windupCued = true;
            }
            if (!windupCued && ctx.ChangeCounter >= CruiserDirector.ResidueSoundFrame) {
                windupCued = true;
                CEUtils.PlaySound("voidSound", CruiserDirector.ResidueSoundPitch, npc.Center);
            }
            if (!burstCued && CuePassed(ctx.ChangeCounter, CruiserDirector.ResidueBurstFrame)) {
                burstCued = true;
            }
            if (!burstCued && ctx.ChangeCounter >= CruiserDirector.ResidueBurstFrame) {
                burstCued = true;
                if (IsServer) {
                    for (int i = 0; i < CruiserDirector.ResidueBurstCount; i++) {
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
            if (ctx.ChangeCounter > CruiserDirector.ResidueLungeStart) {
                npc.velocity += npc.rotation.ToRotationVector2() * CruiserDirector.ResidueLungeThrust;
                npc.velocity *= CruiserDirector.ResidueLungeDrag;
            }
            if (ctx.ChangeCounter > CruiserDirector.ResidueDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
