using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 转死兆星:清掉自己的弹幕,团成球飞回引力场中心,天幕压黑;爆闪拍切形态,之后场地不再移动
    /// 全程不可受伤,没有接触伤害;结束后直接进主序星,死兆星阶段的四段顺次衔接,不经过连接段
    /// </summary>
    [VaultState((int)VSlimeStateIndex.DeathStarShift, typeof(VSlimeStateContext))]
    public class VSlimeDeathStarShiftState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.DeathStarShift;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            VSlimeHostileProjectile.ClearAll();
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = VSlimeDirector.DeathStarShiftBurstFrame;
            ctx.Invulnerable = true;
            ctx.NoContact = true;
            ctx.ArenaFollow = false;

            if (Timer < burst) {
                float p = VSlimeDirector.Progress(Timer, 0, burst);
                Hover(ctx, ctx.ArenaCenter, 26f, 0.14f, 200f);
                ctx.Shape = npc.velocity.Length() > 6f ? VSlimeShape.Orb : VSlimeShape.Slime;
                ctx.SkyDarkness = p;
                ctx.BodyGlow = 0.6f * p;
                ctx.RimGlow = p;
                if (Timer % 2 == 0) {
                    VSlimeVfx.Converge(npc.Center, 140f, 340f);
                }
            }
            else {
                float settle = VSlimeDirector.Progress(Timer, burst, VSlimeDirector.DeathStarShiftDuration - burst);
                ctx.SkyDarkness = 1f;
                ctx.BodyGlow = 1f - settle;
            }

            if (At(1)) {
                VSlimeVfx.Cue("VoidAnticipation", npc.Center, 0.7f, 0.9f, 2);
            }
            if (At(burst)) {
                if (IsServer) {
                    ctx.Phase = VSlimeForm.DeathStar;
                    //没飞到就直接钉到圈心,爆闪盖住这一下换位
                    if (Vector2.Distance(npc.Center, ctx.ArenaCenter) > 24f) {
                        ctx.Owner.Teleport(ctx.ArenaCenter);
                    }
                    VSlimeHostileProjectile.ClearAll();
                    MarkNetUpdate(ctx);
                }
                ctx.Pulse = 1f;
                ctx.ShakeStrength = 1f;
                VSlimeVfx.StarBurst(npc.Center, 5f);
                VSlimeVfx.Shake(npc.Center, 14f);
                VSlimeVfx.Flash(npc.Center, 0.45f);
                VSlimeVfx.SkyFlash(1f);
                VSlimeVfx.Cue("explosionbig", npc.Center, 0.7f, 1f, 2);
            }
            return Timer >= VSlimeDirector.DeathStarShiftDuration ? new VSlimeMainSequenceState() : null;
        }
    }
}
