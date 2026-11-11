using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 终幕:奇点黑洞在 4 秒内缩成一个白点,随后爆出闪耀全场的白光,白光散尽后走 StrikeInstantKill
    /// 收缩的起始半径读 HeldRadius;联机靠击杀包把真死带到各端;全程不可受伤,没有任何判定
    /// </summary>
    [VaultState((int)VSlimeStateIndex.Death, typeof(VSlimeStateContext))]
    public class VSlimeDeathState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.Death;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            ctx.Dying = true;
            ctx.BlinkTimer = 0;
            VSlimeHostileProjectile.ClearAll();
            VSlimeVfx.Cue("blackholeEnd", ctx.Npc.Center, 0.7f, 1f, 2);
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int shrink = VSlimeDirector.DeathShrinkFrames;
            npc.life = Math.Max(1, npc.life);
            ctx.Invulnerable = true;
            ctx.NoContact = true;
            ctx.ArenaFollow = false;
            ctx.Shape = VSlimeShape.BlackHole;
            ctx.HoleTint = 1f;
            ctx.SkyDarkness = 1f;

            if (Timer < shrink) {
                float p = VSlimeDirector.Progress(Timer, 0, shrink);
                float start = Math.Max(ctx.HeldRadius, 60f);
                ctx.HoleRadius = Math.Max(4f, start * (1f - VSlimeDirector.Smooth(p)));
                //越缩越亮,最后只剩一个白点
                ctx.BodyGlow = VSlimeDirector.EaseIn(p);
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f * p);
                if (!Main.dedServ && Timer % 3 == 0) {
                    VSlimeVfx.Converge(npc.Center, ctx.HoleRadius + 60f, ctx.HoleRadius + 420f, DustID.BlueTorch);
                    VSlimeVfx.Shake(npc.Center, 1f + 4f * p);
                }
            }
            else {
                float after = VSlimeDirector.Progress(Timer, shrink, VSlimeDirector.DeathDuration - shrink);
                ctx.HoleRadius = 0f;
                ctx.AlphaDeclared = 0f;
                ctx.BodyGlow = 1f - after;
                ctx.SkyIntensity = 1f - after;
                ctx.SkyDarkness = 1f - after;
            }

            if (At(shrink)) {
                if (IsServer) {
                    ctx.ArenaActive = false;
                    MarkNetUpdate(ctx);
                }
                ctx.Pulse = 1f;
                VSlimeVfx.StarBurst(npc.Center, 7f);
                VSlimeVfx.Scatter(npc.Center, DustID.BlueTorch, 90, 6f, 26f, 1.6f);
                VSlimeVfx.Shake(npc.Center, 20f, 6000f);
                VSlimeVfx.Flash(npc.Center, 1f, 8000f);
                VSlimeVfx.SkyFlash(1f);
                VSlimeVfx.Cue("explosionbig", npc.Center, 0.6f, 1f, 2);
                VSlimeVfx.Cue("crystalShieldBreak", npc.Center, 0.6f, 0.9f, 2);
            }

            if (Timer >= VSlimeDirector.DeathDuration - 1 && IsServer && !ctx.DeathFinished) {
                ctx.DeathFinished = true;
                npc.StrikeInstantKill();
                npc.netSpam = 9;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}
