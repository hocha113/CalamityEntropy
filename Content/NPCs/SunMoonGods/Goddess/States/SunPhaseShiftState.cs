using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 转阶段:清掉自己的弹幕,收翼蓄光,爆闪拍切阶段并把轮换序号归零,全程不可受伤
    /// 新阶段第一手就是表头的签名招
    /// </summary>
    [VaultState((int)SunStateIndex.PhaseShift, typeof(SunStateContext))]
    public class SunPhaseShiftState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.PhaseShift;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SunStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            GodHostileProjectile.ClearAll(GodKind.Sun);
        }

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = SunDirector.ShiftBurstFrame;
            ctx.Invulnerable = true;
            Direct(ctx);
            npc.velocity *= 0.9f;

            if (Timer < burst) {
                float charge = SunDirector.Progress(Timer, 0, burst);
                ctx.BodyGlow = charge;
                ctx.HaloGlow = 0.4f + 0.6f * charge;
                ctx.WingPose = 0.7f * charge;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f * charge);
                ctx.ArmLeft = 1;
                ctx.ArmRight = 1;
                SunVfx.Converge(npc.Center + SunGoddess.HaloOffset, 110f, 240f);
            }
            else {
                float settle = SunDirector.Progress(Timer, burst, SunDirector.ShiftDuration - burst);
                ctx.BodyGlow = 1f - settle;
                ctx.HaloGlow = 1f;
                ctx.WingPose = -1f + settle;
            }

            if (At(0)) {
                SunVfx.Sound(SoundID.Item165 with { Pitch = -0.3f }, npc.Center);
            }
            if (At(burst)) {
                if (IsServer) {
                    ctx.Phase = Math.Min(ctx.Phase + 1, 3);
                    ctx.AttackIndex = 0;
                    GodHostileProjectile.ClearAll(GodKind.Sun);
                    MarkNetUpdate(ctx);
                }
                ctx.HaloPulse = 1f;
                ctx.ShakeStrength = 0.8f;
                SunVfx.Burst(npc.Center, 4f);
                SunVfx.Embers(npc.Center, 50, 5f, 16f, 1.4f);
                SunVfx.Shake(npc.Center, 10f);
                SunVfx.Flash(npc.Center, 0.35f);
                SunVfx.SkyFlash(1f);
                SunVfx.Sound(SoundID.Item161 with { Pitch = -0.2f }, npc.Center);
                SunVfx.Sound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.9f, Pitch = -0.4f }, npc.Center);
            }
            return Timer >= SunDirector.ShiftDuration ? new SunHubState() : null;
        }
    }
}