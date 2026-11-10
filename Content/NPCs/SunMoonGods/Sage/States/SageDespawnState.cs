using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>脱战:收翼朝天上游走并淡出,目标回来就回连接段接着打</summary>
    [VaultState((int)SageStateIndex.Despawn, typeof(SageStateContext))]
    public class SageDespawnState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.Despawn;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SageStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            GodHostileProjectile.ClearAll(GodKind.Moon);
        }

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            if (ctx.TargetValid) {
                return new SageHubState();
            }
            Direct(ctx);
            npc.velocity.Y -= 0.4f;
            npc.velocity *= 0.97f;
            ctx.Face = SageFaceMode.Velocity;
            float fade = 1f - SageDirector.Progress(Timer, 30, SageDirector.NoTargetDespawnFrames - 30);
            ctx.AlphaDeclared = fade;
            ctx.SkyIntensity = fade;
            ctx.CrestGlow = fade * SageDirector.CrestIdle(ctx.Phase);
            ctx.WingSpread = -0.7f;
            if (Timer > SageDirector.NoTargetDespawnFrames && IsServer) {
                npc.active = false;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}