using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 转阶段:清掉自己的弹幕,收翼聚星,爆闪拍切阶段并把轮换序号归零,全程不可受伤
    /// 月轮和天上的月亮跟着盈一档,新阶段第一手就是表头的签名招
    /// </summary>
    [VaultState((int)SageStateIndex.PhaseShift, typeof(SageStateContext))]
    public class SagePhaseShiftState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.PhaseShift;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SageStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            GodHostileProjectile.ClearAll(GodKind.Moon);
        }

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = SageDirector.ShiftBurstFrame;
            ctx.Invulnerable = true;
            Direct(ctx);
            npc.velocity *= 0.9f;

            if (Timer < burst) {
                float charge = SageDirector.Progress(Timer, 0, burst);
                ctx.BodyGlow = charge;
                ctx.CrestGlow = 0.4f + 0.6f * charge;
                ctx.WingSpread = -0.8f * charge;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f * charge);
                SageVfx.Converge(npc.Center, 120f, 260f);
            }
            else {
                float settle = SageDirector.Progress(Timer, burst, SageDirector.ShiftDuration - burst);
                ctx.BodyGlow = 1f - settle;
                ctx.CrestGlow = 1f;
                ctx.WingSpread = 1f - settle;
            }

            if (At(0)) {
                SageVfx.Cue("bell", npc.Center, 0.8f);
            }
            if (At(burst)) {
                if (IsServer) {
                    ctx.Phase = Math.Min(ctx.Phase + 1, 3);
                    ctx.AttackIndex = 0;
                    GodHostileProjectile.ClearAll(GodKind.Moon);
                    MarkNetUpdate(ctx);
                }
                ctx.CrestPulse = 1f;
                ctx.ShakeStrength = 0.8f;
                SageVfx.Burst(npc.Center, 4f);
                SageVfx.Stardust(npc.Center, 50, 5f, 16f, 1.4f);
                SageVfx.Shake(npc.Center, 10f);
                SageVfx.Flash(npc.Center, 0.35f);
                SageVfx.SkyFlash(1f);
                SageVfx.Cue("runesong3", npc.Center, 1f);
                SageVfx.Cue("truemoonlighthit", npc.Center, 0.8f);
            }
            return Timer >= SageDirector.ShiftDuration ? new SageHubState() : null;
        }
    }
}