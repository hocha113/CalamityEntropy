using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 虚空尖刺:速度按 0.08 逼近 46、朝向按 0.0376 比例掰向玩家,于是它是一条大半径的高速弧线。
    /// 40 / 60 / 80 / 100 帧各打一圈 12 发尖刺,150 帧收招。
    /// <para>
    /// <b>二阶段第一手就是它</b>,而且是带着被转阶段打断那一手的残余计数起跑的
    /// (原代码转阶段收尾直写 <c>ai = VoidSpike</c>,不清 <c>changeCounter</c>)。
    /// 残值超过 150 时这一手会在第一帧直接收招、一发不放,那是原版行为
    /// </para>
    /// </summary>
    [VaultState((int)CruiserStateIndex.VoidSpike, typeof(CruiserStateContext))]
    public class CruiserVoidSpikeState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.VoidSpike;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            npc.velocity = npc.velocity.normalize()
                * (npc.velocity.Length() + (CruiserDirector.SpikeSpeedTarget - npc.velocity.Length()) * CruiserDirector.SpikeSpeedLerp);
            npc.velocity = CEUtils.RotateTowardsAngle(npc.velocity.ToRotation(),
                (player.Center - npc.Center).ToRotation(), CruiserDirector.SpikeTurnRate, false).ToRotationVector2()
                * npc.velocity.Length();

            ctx.ChangeCounter++;
            //等值判定在这里是安全的,不必改区间:ChangeCounter 的容差收养写在 ReceiveExtraAI 里,
            //而 NPC 的 SyncNPC 只有客户端会收,权威端的 ChangeCounter 从不被收养、每帧稳定 +1,
            //四个阈值各命中恰好一次。这一拍的整个拍体又都在 IsServer 门内(弹幕 + netUpdate),
            //客户端跨过或重放它都不产生任何效果——没有速度写入,也没有本地演出
            if (ctx.ChangeCounter == CruiserDirector.SpikeRingFrameA
                || ctx.ChangeCounter == CruiserDirector.SpikeRingFrameB
                || ctx.ChangeCounter == CruiserDirector.SpikeRingFrameC
                || ctx.ChangeCounter == CruiserDirector.SpikeRingFrameD) {
                if (IsServer) {
                    for (float i = 0; i < 360; i += CruiserDirector.SpikeRingAngleStep) {
                        Shoot(ctx, ModContent.ProjectileType<VoidSpike>(), npc.Center,
                            MathHelper.ToRadians(i).ToRotationVector2() * CruiserDirector.SpikeRingSpeed);
                    }
                    MarkNetUpdate(ctx);
                }
            }
            if (ctx.ChangeCounter > CruiserDirector.SpikeDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
