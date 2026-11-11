using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 引力星环:本体站定,放出四圈反向慢转的暗物质球(2 秒后才有判定) → 三轮各 6 颗瞄准凝胶球 → 紫边和 64 格预警圈亮 4 秒 → 整圈爆发并摧毁所有暗物质球
    /// 公平阀:每圈相邻两球弧距约 230 像素;爆发半径 64 格,引力场半径 75 格,圈沿始终留着 11 格宽的安全带
    /// </summary>
    [VaultState((int)VSlimeStateIndex.GravityRings, typeof(VSlimeStateContext))]
    public class VSlimeGravityRingsState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.GravityRings;

        private const int BurstBeat = VSlimeDirector.RingChargeStart + VSlimeDirector.RingChargeFrames;

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            if (At(0)) {
                SpawnRings(ctx);
                ctx.Pulse = 0.8f;
                VSlimeVfx.VoidBurst(npc.Center, 2f);
                VSlimeVfx.Cue("VortexSpawn", npc.Center, 1f, 0.7f, 2);
            }

            for (int v = 0; v < VSlimeDirector.RingVolleys; v++) {
                int beat = VSlimeDirector.RingVolleyStart + v * VSlimeDirector.RingVolleyInterval;
                if (At(beat)) {
                    SpawnVolley(ctx, v);
                }
                //成形期间本体略压扁,发射拍弹起
                if (Timer >= beat && Timer < beat + VSlimeDirector.RingVolleyHover) {
                    ctx.Squash = 0.2f * VSlimeDirector.Progress(Timer, beat, VSlimeDirector.RingVolleyHover);
                }
            }

            if (Timer >= VSlimeDirector.RingChargeStart && Timer < BurstBeat) {
                float p = VSlimeDirector.Progress(Timer, VSlimeDirector.RingChargeStart, VSlimeDirector.RingChargeFrames);
                ctx.RimGlow = p;
                ctx.ChargeGlow = p;
                ctx.WarnRadius = VSlimeDirector.RingBurstRadius;
                ctx.WarnStrength = 0.3f + 0.7f * p;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.6f * p);
                ctx.Swell = 0.12f * p;
                if (Timer % 2 == 0) {
                    VSlimeVfx.Converge(npc.Center, 160f, 420f, DustID.RedTorch);
                }
            }
            if (At(VSlimeDirector.RingChargeStart)) {
                VSlimeVfx.Cue("blackholeCharge", npc.Center, 0.8f, 0.9f, 2);
            }
            if (At(BurstBeat)) {
                if (IsServer) {
                    Shoot<VSlimeShockwave>(ctx, npc.Center, Vector2.Zero, VSlimeDirector.DmgBurst, VSlimeShockwave.ModeBurst, npc.whoAmI);
                    VSlimeHostileProjectile.ClearType<VSlimeDarkOrb>();
                }
                ctx.Pulse = 1f;
                ctx.ShakeStrength = 1f;
                VSlimeVfx.VoidBurst(npc.Center, 4f);
                VSlimeVfx.Shake(npc.Center, 14f);
                VSlimeVfx.Flash(npc.Center, 0.3f);
                VSlimeVfx.SkyFlash(0.9f);
                VSlimeVfx.Cue("pulseBlast", npc.Center, 0.8f, 1f, 2);
            }
            return Timer >= BurstBeat + VSlimeDirector.RingTail ? EndAttack(ctx) : null;
        }

        /// <summary>相邻两圈转向相反,每圈起始角由种子定</summary>
        private static void SpawnRings(VSlimeStateContext ctx) {
            if (!IsServer) {
                return;
            }
            NPC npc = ctx.Npc;
            UnifiedRandom rng = Rng(ctx, 0);
            for (int k = 0; k < VSlimeDirector.RingCount; k++) {
                float radius = VSlimeDirector.RingRadiusStep * (k + 1);
                int count = VSlimeDirector.RingOrbsStep * (k + 1);
                int dir = (k % 2 == 0 ? 1 : -1) * ctx.SideDir;
                float baseAngle = rng.NextFloat(MathHelper.TwoPi);
                for (int j = 0; j < count; j++) {
                    float angle = baseAngle + MathHelper.TwoPi * j / count;
                    Shoot<VSlimeDarkOrb>(ctx, npc.Center + angle.ToRotationVector2() * radius, Vector2.Zero, VSlimeDirector.DmgDarkOrb,
                        npc.whoAmI, radius * dir, angle);
                }
            }
        }

        /// <summary>六颗凝胶球围着本体成形,悬停后逐颗朝玩家发射</summary>
        private static void SpawnVolley(VSlimeStateContext ctx, int volley) {
            NPC npc = ctx.Npc;
            ctx.Pulse = Math.Max(ctx.Pulse, 0.5f);
            VSlimeVfx.Sound(SoundID.Item155 with { Pitch = -0.3f, Volume = 0.8f }, npc.Center);
            if (!IsServer) {
                return;
            }
            int count = VSlimeDirector.RingVolleyGels;
            for (int g = 0; g < count; g++) {
                float angle = MathHelper.TwoPi * (g + 0.5f * volley) / count;
                Shoot<VSlimeDarkGel>(ctx, npc.Center + angle.ToRotationVector2() * VSlimeDirector.RingVolleyRadius, Vector2.Zero, VSlimeDirector.DmgDarkGel,
                    VSlimeDarkGel.ModeAimed, VSlimeDirector.RingVolleyHover + g * VSlimeDirector.GelStagger, npc.whoAmI);
            }
        }
    }
}
