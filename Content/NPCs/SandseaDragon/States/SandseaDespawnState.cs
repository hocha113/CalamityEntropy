using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>脱战:朝正下方潜走并淡出,前 90 帧内目标回来就回连接段接着打,边界旋风随本体消失而散</summary>
    [VaultState((int)SandseaStateIndex.Despawn, typeof(SandseaStateContext))]
    public class SandseaDespawnState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.Despawn;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            SandseaHostileProjectile.ClearAll();
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            if (IsServer && ctx.TargetValid && Timer < SandseaDirector.DespawnReturnWindow) {
                return new SandseaHubState();
            }
            ctx.ContactActive = false;
            if (InSand(ctx)) {
                SteerHeading(ctx, MathHelper.PiOver2, 16f);
            }
            else {
                Ballistic(ctx);
            }
            ctx.AlphaDeclared = 1f - SandseaDirector.Progress(Timer, SandseaDirector.DespawnFrames - 40, 40);
            if (Timer > SandseaDirector.DespawnFrames && IsServer) {
                npc.active = false;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}
