using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 日出登场:日轮先亮,本体收着翼从光里淡入,爆闪拍展翼,全程不可受伤也不出招
    /// </summary>
    [VaultState((int)SunStateIndex.Entrance, typeof(SunStateContext))]
    public class SunEntranceState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.Entrance;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = SunDirector.EntranceBurstFrame;
            ctx.Invulnerable = true;
            Direct(ctx);

            if (Timer < burst) {
                float rise = SunDirector.Progress(Timer, 0, burst);
                npc.velocity = new Vector2(0f, MathHelper.Lerp(1.4f, 0f, rise));
                ctx.SkyIntensity = rise;
                ctx.AlphaDeclared = GodDraw.Span(Timer, 30f, 100f);
                ctx.WingPose = 1f;
                ctx.HaloGlow = rise;
                ctx.BodyGlow = 1f - 0.7f * GodDraw.Span(Timer, 50f, burst);
                ctx.ArmLeft = 1;
                ctx.ArmRight = 1;
                if (Timer % 2 == 0) {
                    SunVfx.Converge(npc.Center + SunGoddess.HaloOffset, 120f, 260f);
                }
            }
            else {
                float settle = SunDirector.Progress(Timer, burst, SunDirector.EntranceDuration - burst);
                npc.velocity *= 0.8f;
                ctx.AlphaDeclared = 1f;
                ctx.WingPose = MathHelper.Lerp(-1f, 0f, settle);
                ctx.HaloGlow = Math.Max(ctx.HaloGlow, 1f - 0.5f * settle);
                ctx.BodyGlow = 0.3f * (1f - settle);
                ctx.ArmLeft = settle < 0.3f ? 2 : 0;
                ctx.ArmRight = ctx.ArmLeft;
            }

            if (At(6)) {
                SunVfx.Sound(SoundID.Item161, npc.Center);
            }
            if (At(burst)) {
                ctx.HaloPulse = 1f;
                ctx.ShakeStrength = 0.6f;
                SunVfx.Burst(npc.Center, 3.5f);
                SunVfx.Embers(npc.Center, 40, 4f, 14f, 1.3f);
                SunVfx.Shake(npc.Center, 7f);
                SunVfx.Flash(npc.Center, 0.3f);
                SunVfx.SkyFlash(1f);
                SunVfx.Sound(SoundID.DD2_PhantomPhoenixShot with { Volume = 1f, Pitch = -0.5f }, npc.Center);
            }
            return Timer >= SunDirector.EntranceDuration ? new SunHubState() : null;
        }
    }
}