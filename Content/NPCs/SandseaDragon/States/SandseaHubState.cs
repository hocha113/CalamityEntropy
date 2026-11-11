using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 连接段:开头权威端按表选招写入 PendingState,潜在沙里朝新招的起手点巡航
    /// 这段没有冒头的动作,是整场的喘息;在空中进入时先落回沙里
    /// </summary>
    [VaultState((int)SandseaStateIndex.Hub, typeof(SandseaStateContext))]
    public class SandseaHubState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.Hub;
        public override int TimeoutFrames => 60 * 8;

        /// <summary>按 PendingState 缓存的下一招实例,只用来问起手点,不进状态机</summary>
        private SandseaStateBase probe;
        private int probeId = -1;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            probe = null;
            probeId = -1;
            //客户端往往晚于带 PendingState 的包,只权威端清
            if (IsServer) {
                ctx.PendingState = -1;
            }
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            int total = SandseaDirector.ConnectorFrames(ctx.Phase);
            if (At(0) && IsServer) {
                Pick(ctx);
            }

            SandseaStateBase next = Probe(ctx.PendingState);
            Vector2 anchor = next != null ? next.AnchorFor(ctx) : base.AnchorFor(ctx);
            //Swim 在空中时会先落回沙里,到位后停在起手点上
            Swim(ctx, anchor, SandseaDirector.SwimSpeed);

            //没入沙就不起招,每招的第一拍都假定本体在沙里
            if (!IsServer || Timer < total || !InSand(ctx)) {
                return null;
            }
            if (ctx.PendingState < 0) {
                Pick(ctx);
                return null;
            }
            SandseaStateBase state = SandseaRotation.Create((SandseaStateIndex)ctx.PendingState);
            ctx.PendingState = -1;
            MarkNetUpdate(ctx);
            return state;
        }

        /// <summary>权威端选招,新招从本体当前所在的一侧起手,不从玩家脚下穿过去换边</summary>
        private void Pick(SandseaStateContext ctx) {
            ctx.SideDir = SideOfTarget(ctx);
            ctx.PendingState = (int)SandseaRotation.Pick(ctx);
            ctx.Npc.netUpdate = true;
        }

        private SandseaStateBase Probe(int pendingState) {
            if (pendingState < 0) {
                probe = null;
                probeId = -1;
                return null;
            }
            if (probe == null || probeId != pendingState) {
                probe = SandseaRotation.Create((SandseaStateIndex)pendingState);
                probeId = pendingState;
            }
            return probe;
        }
    }
}
