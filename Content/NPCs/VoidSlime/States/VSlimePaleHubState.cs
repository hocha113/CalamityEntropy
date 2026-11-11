using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 苍白形态的连接段:落地停稳,开头权威端按表选招,停够帧数并且着地才出手
    /// 一直不着地时最多再等 AirWaitMax 帧
    /// </summary>
    [VaultState((int)VSlimeStateIndex.PaleHub, typeof(VSlimeStateContext))]
    public class VSlimePaleHubState : VSlimeStateBase
    {
        private const int AirWaitMax = 90;

        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.PaleHub;
        public override int TimeoutFrames => 60 * 6;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            //客户端往往晚于带 PendingState 的包,只权威端清
            if (IsServer) {
                ctx.PendingState = -1;
            }
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            Ground(ctx);
            npc.velocity.X *= 0.85f;
            ctx.Squash = 0.05f * MathF.Sin(Timer * 0.22f);

            if (At(0) && IsServer) {
                Pick(ctx);
            }
            int total = VSlimeDirector.PaleConnectorFrames;
            if (!IsServer || Timer < total) {
                return null;
            }
            bool grounded = npc.velocity.Y == 0f;
            if (!grounded && Timer < total + AirWaitMax) {
                return null;
            }
            if (ctx.PendingState < 0) {
                Pick(ctx);
                return null;
            }
            VSlimeStateBase state = VSlimeRotation.Create((VSlimeStateIndex)ctx.PendingState);
            ctx.PendingState = -1;
            MarkNetUpdate(ctx);
            return state;
        }

        private static void Pick(VSlimeStateContext ctx) {
            ctx.PendingState = (int)VSlimeRotation.Pick(ctx);
            ctx.SideDir = ctx.Target.Center.X >= ctx.Npc.Center.X ? 1 : -1;
            ctx.Combo = 0;
            ctx.Npc.netUpdate = true;
        }
    }
}
