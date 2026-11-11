using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 脱战:目标死亡或超出 250 格,本体上升淡出,天幕和引力场一起退场
    /// 淡出完之前目标回来就按形态回到对应的起点接着打
    /// </summary>
    [VaultState((int)VSlimeStateIndex.Despawn, typeof(VSlimeStateContext))]
    public class VSlimeDespawnState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.Despawn;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            VSlimeHostileProjectile.ClearAll();
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            if (ctx.TargetValid) {
                return Resume(ctx);
            }
            ctx.NoContact = true;
            ctx.Invulnerable = true;
            ctx.ArenaFollow = false;
            Direct(ctx);
            npc.velocity.X *= 0.95f;
            npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, -9f, 0.04f);
            float fade = 1f - VSlimeDirector.Progress(Timer, 30, VSlimeDirector.NoTargetDespawnFrames - 30);
            ctx.AlphaDeclared = fade;
            ctx.SkyIntensity *= fade;
            ctx.Shape = ctx.Phase <= VSlimeForm.Pale ? VSlimeShape.Slime : VSlimeShape.Orb;
            if (Timer > VSlimeDirector.NoTargetDespawnFrames && IsServer) {
                npc.active = false;
                npc.netUpdate = true;
            }
            return null;
        }

        /// <summary>死兆星阶段没有连接段,从主序星重新演起</summary>
        private static IVaultState<VSlimeStateContext> Resume(VSlimeStateContext ctx) {
            if (ctx.Phase >= VSlimeForm.DeathStar) {
                return new VSlimeMainSequenceState();
            }
            return EndAttack(ctx);
        }
    }
}
