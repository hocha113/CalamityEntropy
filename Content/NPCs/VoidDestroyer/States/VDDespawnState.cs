using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 脱战撤离:目标失效后 60 帧跃迁遁入 Z 6 的深空(与出场起点同一深度,读成「回去了」)并按 190 帧淡出,
    /// 期间目标回来就回 hub(hub 落定拍会把它拉回平面),否则消失。无接触、限制圈关
    /// </summary>
    [VaultState((int)VDStateIndex.Despawn, typeof(VDStateContext))]
    public class VDDespawnState : VDStateBase
    {
        public override string StateName => "Despawn";
        public override VDStateIndex StateIndex => VDStateIndex.Despawn;
        public override bool ContactByDefault => false;
        public override bool RunsDuringBlink => true;
        public override int TimeoutFrames => int.MaxValue;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            ctx.QueuedChainState = -1;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            ctx.ArenaActive = false;
            DeclareDirect(ctx);
            npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0, -14f), 0.04f);
            float fade = MathHelper.Clamp(1f - Timer / (float)VDDirector.NoTargetDespawnFrames, 0f, 1f);
            DeclareAlpha(ctx, fade, 1f);
            //跃迁遁入深处:立方缓出,起步猛、越远越慢
            ctx.Depth = VDDirector.DespawnDepth * VDDepth.RetreatCurve(Timer / (float)VDDirector.DespawnWarpFrames);
            if (Timer == 1) {
                VDVfx.Sound("vbdisapear", 0.8f, ctx.Owner.ProjectedCenter, 2);
            }

            if (ctx.TargetValid) {
                return new VDHubState();
            }
            if (Timer > VDDirector.NoTargetDespawnFrames && IsServer) {
                npc.active = false;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}
