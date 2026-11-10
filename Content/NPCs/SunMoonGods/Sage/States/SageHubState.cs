using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 连接段:开头权威端按表选招写入 PendingState,飞向新招的锚点,远了就星遁过去
    /// 最后一拍月轮亮起、双翼展开,是全招通用的出手信号;场上弹幕还密就再等一会
    /// </summary>
    [VaultState((int)SageStateIndex.Hub, typeof(SageStateContext))]
    public class SageHubState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.Hub;
        public override int TimeoutFrames => 60 * 8;

        /// <summary>按 PendingState 缓存的下一招实例,只用来问锚点,不进状态机</summary>
        private SageStateBase probe;
        private int probeId = -1;

        public override void OnEnter(SageStateContext ctx) {
            base.OnEnter(ctx);
            probe = null;
            probeId = -1;
            //客户端往往晚于带 PendingState 的包,只权威端清
            if (IsServer) {
                ctx.PendingState = -1;
            }
        }

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            int total = SageDirector.ConnectorFrames(ctx.Phase);
            int postureStart = total - SageDirector.ConnectorPostureFrames;

            if (At(0) && IsServer) {
                PickAndPosition(ctx);
            }

            SageStateBase next = Probe(ctx.PendingState);
            Vector2 anchor = next != null
                ? next.AnchorFor(ctx)
                : ctx.Target.Center + new Vector2(ctx.SideDir * SageDirector.DefaultAnchor.X, SageDirector.DefaultAnchor.Y);

            if (Timer < postureStart) {
                Hover(ctx, anchor, SageDirector.ConnectorFlySpeed, 0.12f, 160f);
            }
            else {
                Hover(ctx, anchor, SageDirector.ConnectorFlySpeed * 0.5f, 0.1f, 160f);
                float p = SageDirector.Progress(Timer, postureStart, SageDirector.ConnectorPostureFrames);
                ctx.CrestGlow = Math.Max(ctx.CrestGlow, 0.45f + 0.5f * p);
                ctx.WingSpread = 0.7f * p;
            }

            if (!IsServer || Timer < total || ctx.BlinkTimer > 0) {
                return null;
            }
            //杂波阀:上一招的弹幕还密就再等一会
            if (GodHostileProjectile.CountAlive(GodKind.Moon) > SageDirector.ClutterThreshold && Timer < total + SageDirector.ClutterWaitMax) {
                return null;
            }
            if (ctx.PendingState < 0) {
                PickAndPosition(ctx);
                return null;
            }
            SageStateBase state = SageRotation.Create((SageStateIndex)ctx.PendingState);
            ctx.PendingState = -1;
            MarkNetUpdate(ctx);
            return state;
        }

        /// <summary>权威端选招,每手翻转侧向,新招锚点远了就星遁过去</summary>
        private void PickAndPosition(SageStateContext ctx) {
            SageStateIndex pick = SageRotation.Pick(ctx);
            ctx.SideDir = -ctx.SideDir;
            ctx.PendingState = (int)pick;
            SageStateBase next = Probe(ctx.PendingState);
            if (next == null) {
                ctx.PendingState = -1;
                return;
            }
            next.OnPicked(ctx);
            Vector2 anchor = next.AnchorFor(ctx);
            if (Vector2.Distance(anchor, ctx.Npc.Center) > SageDirector.ConnectorBlinkDistance) {
                ctx.Owner.StartBlink(anchor);
            }
            ctx.Npc.netUpdate = true;
        }

        private SageStateBase Probe(int pendingState) {
            if (pendingState < 0) {
                probe = null;
                probeId = -1;
                return null;
            }
            if (probe == null || probeId != pendingState) {
                probe = SageRotation.Create((SageStateIndex)pendingState);
                probeId = pendingState;
            }
            return probe;
        }
    }
}