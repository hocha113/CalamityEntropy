using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 转奇点形态:清掉自己的弹幕,头顶淡紫虹光成环 → 身体溅散露出中心的奇点 → 爆闪拍切形态并把轮换序号归零 → 重聚成奇点史莱姆
    /// 全程不可受伤,没有接触伤害
    /// </summary>
    [VaultState((int)VSlimeStateIndex.SingularityShift, typeof(VSlimeStateContext))]
    public class VSlimeSingularityShiftState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.SingularityShift;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            VSlimeHostileProjectile.ClearAll();
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int haloEnd = VSlimeDirector.SingularityShiftHaloFrames;
            int burst = VSlimeDirector.SingularityShiftBurstFrame;
            ctx.Invulnerable = true;
            ctx.NoContact = true;

            if (Timer < haloEnd) {
                float p = VSlimeDirector.Progress(Timer, 0, haloEnd);
                ctx.Halo = VSlimeDirector.EaseOut(p);
                ctx.RimGlow = 0.6f * p;
                ctx.Squash = 0.12f * MathF.Sin(p * MathHelper.Pi);
            }
            else if (Timer < burst) {
                float p = VSlimeDirector.Progress(Timer, haloEnd, burst - haloEnd);
                ctx.Halo = 1f;
                ctx.RimGlow = 0.6f + 0.4f * p;
                ctx.Splatter = VSlimeDirector.EaseIn(p);
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.7f * p);
                if (Timer % 2 == 0) {
                    VSlimeVfx.Converge(npc.Center, 120f, 300f);
                }
            }
            else {
                float settle = VSlimeDirector.Progress(Timer, burst, VSlimeDirector.SingularityShiftDuration - burst);
                ctx.Halo = 1f - settle;
                ctx.Splatter = 1f - VSlimeDirector.EaseOut(settle);
                ctx.BodyGlow = 1f - settle;
            }

            if (At(1)) {
                VSlimeVfx.Cue("soulshine", npc.Center, 0.8f, 0.9f, 2);
            }
            if (At(haloEnd)) {
                VSlimeVfx.Cue("blackholeCharge", npc.Center, 1.2f, 0.8f, 2);
            }
            if (At(burst)) {
                if (IsServer) {
                    ctx.Phase = VSlimeForm.Singularity;
                    ctx.AttackIndex = 0;
                    VSlimeHostileProjectile.ClearAll();
                    MarkNetUpdate(ctx);
                }
                ctx.Pulse = 1f;
                ctx.ShakeStrength = 0.9f;
                VSlimeVfx.VoidBurst(npc.Center, 4.5f);
                VSlimeVfx.Stardust(npc.Center, 50, 4f, 15f, 1.3f);
                VSlimeVfx.Shake(npc.Center, 12f);
                VSlimeVfx.Flash(npc.Center, 0.4f);
                VSlimeVfx.SkyFlash(1f);
                VSlimeVfx.Cue("BlackholeSpawn", npc.Center, 0.7f, 1f, 2);
                VSlimeVfx.Sound(SoundID.Item167 with { Pitch = -0.5f }, npc.Center);
            }
            return Timer >= VSlimeDirector.SingularityShiftDuration ? new VSlimeHubState() : null;
        }
    }
}
