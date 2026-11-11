using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 死亡演出:每 10 帧从尾到身炸散一节,60 帧时只剩的头部化成黯淡的尸块滚落,随后走 StrikeInstantKill 掉落战利品
    /// 联机靠击杀包把真死带到各端,尸块和沙尘由各客户端按过线的计时自己放
    /// </summary>
    [VaultState((int)SandseaStateIndex.Death, typeof(SandseaStateContext))]
    public class SandseaDeathState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.Death;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            ctx.Dying = true;
            SandseaHostileProjectile.ClearAll();
            SandseaVfx.Sound(SoundID.DD2_BetsyDeath with { Pitch = -0.4f, Volume = 0.9f }, ctx.Npc.Center);
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            SandseaDragonHead owner = ctx.Owner;
            npc.life = Math.Max(1, npc.life);
            ctx.Invulnerable = true;
            ctx.ContactActive = false;
            ctx.AlphaDeclared = 1f;
            ctx.Effort = 0.2f;
            ctx.JawOpen = 0.8f;
            ctx.Mode = SandseaMoveMode.Direct;
            npc.velocity *= 0.94f;
            npc.velocity.Y += 0.14f;

            int dropAt = SandseaDirector.DeathHeadDropFrame;
            int interval = SandseaDirector.DeathSegmentInterval;
            int bodyParts = SandseaDirector.SegmentCount - 1;
            int gone = Timer >= dropAt ? SandseaDirector.SegmentCount : Timer < 8 ? 0 : Math.Min((Timer - 8) / interval + 1, bodyParts);
            ctx.SegmentsGone = gone;

            for (int i = 1; i <= bodyParts; i++) {
                if (At(8 + (i - 1) * interval)) {
                    //第 i 次炸掉的是从尾数起的第 i 节
                    int seg = SandseaDirector.SegmentCount - i;
                    Vector2 at = (owner.Joints[seg] + owner.Joints[seg + 1]) * 0.5f;
                    SandseaVfx.SandBurst(at, 2.4f);
                    SandseaVfx.BreachSound(at, 0.7f);
                    SandseaVfx.Shake(at, 4f, 2000f);
                }
            }

            if (At(dropAt) && !Main.dedServ) {
                SandseaVfx.SandBurst(npc.Center, 2f);
                Vector2 topLeft = npc.Center - SandseaDirector.JointFront[0];
                if (owner.Mod.TryFind("SandseaDragonHeadGore", out ModGore headGore)) {
                    Gore.NewGore(npc.GetSource_Death(), topLeft, npc.velocity * 0.6f + new Vector2(owner.FacingDir * 1.5f, -2f), headGore.Type);
                }
            }

            if (Timer >= SandseaDirector.DeathDuration - 1 && IsServer && !ctx.DeathFinished) {
                ctx.DeathFinished = true;
                npc.StrikeInstantKill();
                npc.netSpam = 9;
                npc.netUpdate = true;
            }
            return null;
        }
    }
}
