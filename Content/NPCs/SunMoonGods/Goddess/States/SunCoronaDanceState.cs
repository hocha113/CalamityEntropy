using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 日冕之舞,对应原版太阳舞:以日轮为心一波波放出等角光芒,先细线预告再点亮慢转
    /// 第一波把玩家放在两道光芒正中,之后每波错半格并反向转,上一波的空隙就是下一波的预告线
    /// </summary>
    [VaultState((int)SunStateIndex.CoronaDance, typeof(SunStateContext))]
    public class SunCoronaDanceState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.CoronaDance;

        public override Vector2 AnchorFor(SunStateContext ctx) => ctx.Target.Center + SunDirector.CoronaAnchor;

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            int rays = SunDirector.CoronaRays(ctx.Phase);
            int waves = SunDirector.CoronaWaves(ctx.Phase);
            int lastWave = SunDirector.CoronaWindup + (waves - 1) * SunDirector.CoronaWaveInterval;
            int castEnd = lastWave + SunDirector.RayTelegraphFrames + SunDirector.RayActiveFrames;

            if (Timer < SunDirector.CoronaWindup) {
                Hover(ctx, AnchorFor(ctx), 16f, 0.1f, 140f);
                if (Timer % 2 == 0) {
                    SunVfx.Converge(ctx.Npc.Center + SunGoddess.HaloOffset, 90f, 200f);
                }
            }
            else {
                //光芒根部跟着本体,这段只慢漂
                Hover(ctx, AnchorFor(ctx), SunDirector.CoronaDriftSpeed, 0.06f, 80f);
            }

            float cast = Timer < castEnd ? SunDirector.Progress(Timer, 0, SunDirector.CoronaWindup) : 0f;
            int arm = Timer < castEnd ? (Timer < 6 ? 2 : 4) : (Timer < castEnd + 6 ? 2 : 0);
            ctx.ArmLeft = arm;
            ctx.ArmRight = arm;
            ctx.CastLeft = cast;
            ctx.CastRight = cast;
            ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.5f + 0.5f * cast);
            ctx.WingPose = -0.7f * cast;

            for (int k = 0; k < waves; k++) {
                if (At(SunDirector.CoronaWindup + k * SunDirector.CoronaWaveInterval)) {
                    FireWave(ctx, k, rays);
                }
            }
            return Timer >= lastWave + SunDirector.RayLife + SunDirector.CoronaTail ? EndAttack(ctx) : null;
        }

        private static void FireWave(SunStateContext ctx, int wave, int rays) {
            NPC npc = ctx.Npc;
            Vector2 center = npc.Center + SunGoddess.HaloOffset;
            if (IsServer) {
                if (wave == 0) {
                    ctx.LockedAngle = (ctx.Target.Center - center).ToRotation() + MathHelper.Pi / rays;
                    MarkNetUpdate(ctx);
                }
                float mode = (wave % 2 == 0 ? 1f : -1f) * SunCoronaRay.ModeDance;
                for (int i = 0; i < rays; i++) {
                    float angle = ctx.LockedAngle + wave * MathHelper.Pi / rays + MathHelper.TwoPi * i / rays;
                    Shoot<SunCoronaRay>(ctx, center, Vector2.Zero, SunDirector.DmgRay, angle, npc.whoAmI, mode);
                }
            }
            ctx.HaloPulse = 0.8f;
            SunVfx.Burst(center, 1.2f);
            SunVfx.Sound(SoundID.Item159 with { MaxInstances = 2 }, npc.Center);
        }
    }
}