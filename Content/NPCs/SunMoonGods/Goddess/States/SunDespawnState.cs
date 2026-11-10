using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>脱战:收翼上升淡出,目标回来就回连接段接着打</summary>
    [VaultState((int)SunStateIndex.Despawn, typeof(SunStateContext))]
    public class SunDespawnState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.Despawn;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SunStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            GodHostileProjectile.ClearAll(GodKind.Sun);
        }

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            NPC npc = ctx.Npc;
            if (ctx.TargetValid) {
                return new SunHubState();
            }
            Direct(ctx);
            npc.velocity.Y -= 0.35f;
            npc.velocity *= 0.97f;
            float fade = 1f - SunDirector.Progress(Timer, 30, SunDirector.NoTargetDespawnFrames - 30);
            ctx.AlphaDeclared = fade;
            ctx.SkyIntensity = fade;
            ctx.HaloGlow = fade * SunDirector.HaloIdle(ctx.Phase);
            ctx.WingPose = 0.6f;
            if (Timer > SunDirector.NoTargetDespawnFrames && IsServer) {
                npc.active = false;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}