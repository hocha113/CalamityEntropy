using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 死亡演出:光柱从体内逐道迸出,超新星拍整个燃尽,余烬散完后走 StrikeInstantKill
    /// 联机靠击杀包把真死带到各端
    /// </summary>
    [VaultState((int)SunStateIndex.Death, typeof(SunStateContext))]
    public class SunDeathState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.Death;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SunStateContext ctx) {
            base.OnEnter(ctx);
            ctx.Dying = true;
            ctx.BlinkTimer = 0;
            GodHostileProjectile.ClearAll(GodKind.Sun);
            SunVfx.Sound(SoundID.Item165 with { Pitch = -0.5f }, ctx.Npc.Center);
        }

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            NPC npc = ctx.Npc;
            int nova = SunDirector.DeathNovaFrame;
            npc.life = Math.Max(1, npc.life);
            ctx.Invulnerable = true;
            Direct(ctx);
            npc.velocity *= 0.9f;

            if (Timer < nova) {
                float p = SunDirector.Progress(Timer, 0, nova);
                ctx.DeathRays = p;
                ctx.BodyGlow = p;
                ctx.HaloGlow = 0.5f + 0.5f * p;
                ctx.WingPose = -p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, p);
                ctx.ArmLeft = 1;
                ctx.ArmRight = 1;
                //越到后面爆点越密
                int interval = Math.Max(4, 14 - Timer / 10);
                if (!Main.dedServ && Timer % interval == 0) {
                    Vector2 pos = npc.Center + Main.rand.NextVector2Circular(60f, 80f);
                    SunVfx.Burst(pos, Main.rand.NextFloat(0.7f, 1.2f));
                    SunVfx.Sound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.45f, PitchVariance = 0.3f, MaxInstances = 4 }, pos);
                    SunVfx.Shake(npc.Center, 2f + 4f * p);
                }
            }
            else {
                float after = SunDirector.Progress(Timer, nova, SunDirector.DeathDuration - nova);
                ctx.AlphaDeclared = 1f - SunDirector.Progress(Timer, nova, 16);
                ctx.DeathRays = 1f - after;
                ctx.BodyGlow = 1f;
                ctx.HaloGlow = 1f - after;
                ctx.SkyIntensity = 1f - after;
            }

            if (At(nova)) {
                ctx.HaloPulse = 1f;
                SunVfx.Burst(npc.Center, 6f);
                SunVfx.Embers(npc.Center, 90, 5f, 20f, 1.5f);
                SunVfx.Shake(npc.Center, 14f);
                SunVfx.Flash(npc.Center, 0.5f);
                SunVfx.SkyFlash(1f);
                SunVfx.Sound(SoundID.DD2_BetsyFireballImpact with { Volume = 1f, Pitch = -0.6f }, npc.Center);
                SunVfx.Sound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.3f }, npc.Center);
            }

            if (Timer >= SunDirector.DeathDuration - 1 && IsServer && !ctx.DeathFinished) {
                ctx.DeathFinished = true;
                npc.StrikeInstantKill();
                npc.netSpam = 9;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}