using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 星门登场:星尘先向一点收束,本体收着翼从星光里淡入,爆闪拍展翼,全程不可受伤也不出招
    /// </summary>
    [VaultState((int)SageStateIndex.Entrance, typeof(SageStateContext))]
    public class SageEntranceState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.Entrance;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = SageDirector.EntranceBurstFrame;
            ctx.Invulnerable = true;
            Direct(ctx);

            if (Timer < burst) {
                float rise = SageDirector.Progress(Timer, 0, burst);
                npc.velocity = new Vector2(0f, MathHelper.Lerp(1.2f, 0f, rise));
                FaceTo(ctx, MathHelper.PiOver2);
                ctx.SkyIntensity = rise;
                ctx.AlphaDeclared = GodDraw.Span(Timer, 34f, 100f);
                ctx.WingSpread = -1f;
                ctx.CrestGlow = rise;
                ctx.BodyGlow = 1f - 0.8f * GodDraw.Span(Timer, 50f, burst);
                if (Timer % 2 == 0) {
                    SageVfx.Converge(npc.Center, 130f, 280f);
                }
            }
            else {
                float settle = SageDirector.Progress(Timer, burst, SageDirector.EntranceDuration - burst);
                npc.velocity *= 0.8f;
                ctx.AlphaDeclared = 1f;
                ctx.WingSpread = 1f - settle;
                ctx.CrestGlow = Math.Max(ctx.CrestGlow, 1f - 0.5f * settle);
                ctx.BodyGlow = 0.2f * (1f - settle);
            }

            if (At(6)) {
                SageVfx.Cue("runesong1", npc.Center, 0.9f);
            }
            if (At(burst)) {
                ctx.CrestPulse = 1f;
                ctx.ShakeStrength = 0.6f;
                SageVfx.Burst(npc.Center, 3.5f);
                SageVfx.Stardust(npc.Center, 40, 4f, 14f, 1.3f);
                SageVfx.Shake(npc.Center, 7f);
                SageVfx.Flash(npc.Center, 0.3f);
                SageVfx.SkyFlash(1f);
                SageVfx.Cue("truemoonlighthit", npc.Center, 0.8f);
            }
            return Timer >= SageDirector.EntranceDuration ? new SageHubState() : null;
        }
    }
}