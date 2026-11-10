using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 死亡演出:体内的星逐颗点亮炸开,十字星芒越拉越长,星爆拍整个散成星尘,散尽后走 StrikeInstantKill
    /// 联机靠击杀包把真死带到各端
    /// </summary>
    [VaultState((int)SageStateIndex.Death, typeof(SageStateContext))]
    public class SageDeathState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.Death;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SageStateContext ctx) {
            base.OnEnter(ctx);
            ctx.Dying = true;
            ctx.BlinkTimer = 0;
            GodHostileProjectile.ClearAll(GodKind.Moon);
            SageVfx.Cue("prophet_death", ctx.Npc.Center, 1.1f);
        }

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            int nova = SageDirector.DeathNovaFrame;
            npc.life = Math.Max(1, npc.life);
            ctx.Invulnerable = true;
            Direct(ctx);
            npc.velocity *= 0.9f;
            FaceTo(ctx, npc.rotation);

            if (Timer < nova) {
                float p = SageDirector.Progress(Timer, 0, nova);
                ctx.DeathStars = p;
                ctx.BodyGlow = p;
                ctx.CrestGlow = 0.5f + 0.5f * p;
                ctx.WingSpread = p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, p);
                //越到后面爆点越密
                int interval = Math.Max(4, 14 - Timer / 10);
                if (!Main.dedServ && Timer % interval == 0) {
                    Vector2 pos = npc.Center + Main.rand.NextVector2Circular(110f, 110f);
                    SageVfx.Burst(pos, Main.rand.NextFloat(0.7f, 1.2f));
                    SageVfx.Cue("moonlighthit" + Main.rand.Next(2), pos, Main.rand.NextFloat(0.9f, 1.3f), 0.45f, 4);
                    SageVfx.Shake(npc.Center, 2f + 4f * p);
                }
            }
            else {
                float after = SageDirector.Progress(Timer, nova, SageDirector.DeathDuration - nova);
                ctx.AlphaDeclared = 1f - SageDirector.Progress(Timer, nova, 16);
                ctx.DeathStars = 1f - after;
                ctx.BodyGlow = 1f;
                ctx.CrestGlow = 1f - after;
                ctx.SkyIntensity = 1f - after;
            }

            if (At(nova)) {
                ctx.CrestPulse = 1f;
                SageVfx.Burst(npc.Center, 6f);
                SageVfx.Stardust(npc.Center, 90, 5f, 20f, 1.5f);
                SageVfx.Shake(npc.Center, 14f);
                SageVfx.Flash(npc.Center, 0.5f);
                SageVfx.SkyFlash(1f);
                SageVfx.Cue("truemoonlighthit", npc.Center, 0.7f);
                SageVfx.Cue("rune_shatter", npc.Center, 0.8f);
            }

            if (Timer >= SageDirector.DeathDuration - 1 && IsServer && !ctx.DeathFinished) {
                ctx.DeathFinished = true;
                npc.StrikeInstantKill();
                npc.netSpam = 9;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}