using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 连接段:开头权威端按表选招写入 PendingState,飞向新招的锚点,太远就化光闪过去
    /// 最后一拍日轮亮起、双翼扬起,是全招通用的出手信号;场上弹幕还密就再等一会
    /// </summary>
    [VaultState((int)SunStateIndex.Hub, typeof(SunStateContext))]
    public class SunHubState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.Hub;
        public override int TimeoutFrames => 60 * 8;

        /// <summary>按 PendingState 缓存的下一招实例,只用来问锚点,不进状态机</summary>
        private SunStateBase probe;
        private int probeId = -1;

        public override void OnEnter(SunStateContext ctx) {
            base.OnEnter(ctx);
            probe = null;
            probeId = -1;
            //客户端往往晚于带 PendingState 的包,只权威端清
            if (IsServer) {
                ctx.PendingState = -1;
            }
        }

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            int total = SunDirector.ConnectorFrames(ctx.Phase);
            int postureStart = total - SunDirector.ConnectorPostureFrames;

            if (At(0) && IsServer) {
                PickAndPosition(ctx);
            }

            SunStateBase next = Probe(ctx.PendingState);
            Vector2 anchor = next != null
                ? next.AnchorFor(ctx)
                : ctx.Target.Center + new Vector2(ctx.SideDir * SunDirector.DefaultAnchor.X, SunDirector.DefaultAnchor.Y);

            if (Timer < postureStart) {
                Hover(ctx, anchor, SunDirector.ConnectorFlySpeed, 0.12f, 160f);
            }
            else {
                Hover(ctx, anchor, SunDirector.ConnectorFlySpeed * 0.5f, 0.1f, 160f);
                float p = SunDirector.Progress(Timer, postureStart, SunDirector.ConnectorPostureFrames);
                ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.45f + 0.5f * p);
                ctx.WingPose = -0.7f * p;
            }

            if (!IsServer || Timer < total || ctx.BlinkTimer > 0) {
                return null;
            }
            //杂波阀:上一招的弹幕还密就再等一会
            if (GodHostileProjectile.CountAlive(GodKind.Sun) > SunDirector.ClutterThreshold && Timer < total + SunDirector.ClutterWaitMax) {
                return null;
            }
            if (ctx.PendingState < 0) {
                PickAndPosition(ctx);
                return null;
            }
            SunStateBase state = SunRotation.Create((SunStateIndex)ctx.PendingState);
            ctx.PendingState = -1;
            MarkNetUpdate(ctx);
            return state;
        }

        /// <summary>权威端选招,每手翻转侧向,新招锚点太远就闪过去</summary>
        private void PickAndPosition(SunStateContext ctx) {
            SunStateIndex pick = SunRotation.Pick(ctx);
            ctx.SideDir = -ctx.SideDir;
            ctx.PendingState = (int)pick;
            SunStateBase next = Probe(ctx.PendingState);
            if (next == null) {
                ctx.PendingState = -1;
                return;
            }
            next.OnPicked(ctx);
            Vector2 anchor = next.AnchorFor(ctx);
            if (Vector2.Distance(anchor, ctx.Npc.Center) > SunDirector.ConnectorBlinkDistance) {
                ctx.Owner.StartBlink(anchor);
            }
            ctx.Npc.netUpdate = true;
        }

        private SunStateBase Probe(int pendingState) {
            if (pendingState < 0) {
                probe = null;
                probeId = -1;
                return null;
            }
            if (probe == null || probeId != pendingState) {
                probe = SunRotation.Create((SunStateIndex)pendingState);
                probeId = pendingState;
            }
            return probe;
        }
    }
}