using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 转二阶段:清掉自己的弹幕,游到玩家侧上方的展示位,战吼拍背鳍亮起、眼放蓝光并切阶段,随后下潜
    /// 全程不可受伤也没有接触伤害,轮换序号归零
    /// </summary>
    [VaultState((int)SandseaStateIndex.PhaseShift, typeof(SandseaStateContext))]
    public class SandseaPhaseShiftState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.PhaseShift;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            SandseaHostileProjectile.ClearAll();
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = SandseaDirector.ShiftBurstFrame;
            ctx.Invulnerable = true;
            ctx.ContactActive = false;

            if (Timer < burst) {
                float x = ctx.Target.Center.X + SideOfTarget(ctx) * 420f;
                Vector2 display = new Vector2(x, SurfaceAt(ctx, x) - SandseaDirector.ShiftDisplayHeight);
                Steer(ctx, display, 15f, 0.8f, 200f);
                float charge = SandseaDirector.Progress(Timer, 0, burst);
                ctx.GlowBoost = 0.5f * charge;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.4f * charge);
            }
            else if (Timer < burst + 30) {
                //战吼时慢慢前滑,张嘴、身体绷住
                SteerHeading(ctx, Heading(ctx), 3f, 1.2f);
                ctx.JawOpen = 1f;
                ctx.GlowBoost = 1f;
                ctx.Effort = 0.4f;
            }
            else {
                DiveIntoSand(ctx);
                ctx.GlowBoost = 1f - SandseaDirector.Progress(Timer, burst + 30, 30) * 0.25f;
            }

            if (At(burst)) {
                if (IsServer) {
                    ctx.Phase = Math.Max(ctx.Phase, 2);
                    ctx.AttackIndex = 0;
                    SandseaHostileProjectile.ClearAll();
                    MarkNetUpdate(ctx);
                }
                ctx.EyeFlash = 1f;
                ctx.ShakeStrength = 1f;
                SandseaVfx.Roar(npc.Center, true);
                SandseaVfx.Ring(npc.Center, SandseaVfx.FinGlow, 3.2f, 24);
                SandseaVfx.Shake(npc.Center, 10f);
                for (int k = 0; k <= SandseaDirector.SegmentCount; k++) {
                    SandseaVfx.FinSparks(ctx.Owner.Joints[k], 8, 9f);
                }
            }

            if (IsServer && Timer >= SandseaDirector.ShiftDuration && (InSand(ctx) || Timer >= SandseaDirector.ShiftDuration + 90)) {
                return new SandseaHubState();
            }
            return null;
        }
    }
}
