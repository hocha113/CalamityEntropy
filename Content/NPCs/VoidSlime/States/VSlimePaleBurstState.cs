using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 破壳:苍白史莱姆锁在 1 血,浮起膨胀,裂纹里透出紫光,爆开拍换成虚空形态
    /// 爆开拍在权威端切形态、回满血、展开引力场;全程不可受伤,没有接触伤害
    /// </summary>
    [VaultState((int)VSlimeStateIndex.PaleBurst, typeof(VSlimeStateContext))]
    public class VSlimePaleBurstState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.PaleBurst;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            ctx.BlinkTimer = 0;
            VSlimeHostileProjectile.ClearAll();
            VSlimeVfx.Cue("crack", ctx.Npc.Center, 0.8f, 0.9f, 2);
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            int burst = VSlimeDirector.BurstSwellFrames;
            ctx.Invulnerable = true;
            ctx.NoContact = true;
            Direct(ctx);

            if (Timer < burst) {
                float p = VSlimeDirector.Progress(Timer, 0, burst);
                npc.velocity = new Vector2(0f, -VSlimeDirector.BurstRise / burst);
                ctx.Swell = 0.42f * VSlimeDirector.EaseIn(p);
                ctx.Cracks = p;
                ctx.BodyGlow = 0.5f * p;
                ctx.RimGlow = p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.2f + 0.8f * p);
                ctx.SkyIntensity = 0.7f * p;
                //越到后面裂响越密
                int interval = Math.Max(6, 30 - Timer / 6);
                if (!Main.dedServ && Timer % interval == 0) {
                    VSlimeVfx.Cue("crack", npc.Center, 1f + 0.5f * p, 0.5f, 3);
                    VSlimeVfx.PaleShards(npc.Center, 3, 1f, 5f);
                    VSlimeVfx.Shake(npc.Center, 2f + 3f * p);
                }
                if (Timer % 2 == 0) {
                    VSlimeVfx.Converge(npc.Center, 120f, 300f);
                }
            }
            else {
                float settle = VSlimeDirector.Progress(Timer, burst, VSlimeDirector.BurstDuration - burst);
                npc.velocity *= 0.85f;
                ctx.Swell = 0.3f * (1f - VSlimeDirector.EaseOut(settle));
                ctx.BodyGlow = 1f - settle;
                ctx.RimGlow = 1f - settle;
                ctx.SkyIntensity = 0.7f + 0.3f * settle;
            }

            if (At(burst)) {
                if (IsServer) {
                    ctx.Phase = VSlimeForm.Void;
                    ctx.AttackIndex = 0;
                    ctx.Owner.OpenArena();
                    ctx.Owner.RefillLife();
                    MarkNetUpdate(ctx);
                }
                ctx.Pulse = 1f;
                ctx.ShakeStrength = 1f;
                VSlimeVfx.PaleShards(npc.Center, 90, 4f, 18f);
                VSlimeVfx.VoidBurst(npc.Center, 5f);
                VSlimeVfx.Stardust(npc.Center, 60, 4f, 16f, 1.4f);
                VSlimeVfx.Shake(npc.Center, 16f);
                VSlimeVfx.Flash(npc.Center, 0.5f);
                VSlimeVfx.SkyFlash(1f);
                VSlimeVfx.Cue("crystalShieldBreak", npc.Center, 0.8f, 1f, 2);
                VSlimeVfx.Cue("explosionbig", npc.Center, 0.9f, 0.9f, 2);
                VSlimeVfx.Sound(SoundID.Item167 with { Pitch = -0.6f }, npc.Center);
            }
            return Timer >= VSlimeDirector.BurstDuration ? new VSlimeHubState() : null;
        }
    }
}
