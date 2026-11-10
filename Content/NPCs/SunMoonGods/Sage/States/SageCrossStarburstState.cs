using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Sage;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 十字星爆,取自星月套装的十字星爆:每波在玩家身上和四周亮出十字标记,预告走完各自沿四向爆成星芒
    /// 偶数波的中心点在玩家当前位置,奇数波在预判点;正十字和斜十字逐波交替,外圈四枚和中心反着来
    /// 上一波爆开时下一波的标记已经亮着
    /// </summary>
    [VaultState((int)SageStateIndex.CrossStarburst, typeof(SageStateContext))]
    public class SageCrossStarburstState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.CrossStarburst;

        public override Vector2 AnchorFor(SageStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SageDirector.CrossAnchor.X, SageDirector.CrossAnchor.Y);

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            int waves = SageDirector.CrossWaves(ctx.Phase);
            int lastWave = SageDirector.CrossWindup + (waves - 1) * SageDirector.CrossWaveInterval;
            int castEnd = lastWave + SageDirector.CrossTelegraph;

            Hover(ctx, AnchorFor(ctx), SageDirector.HoverSpeed * 0.7f, 0.07f, 140f);
            float cast = Timer < castEnd ? SageDirector.Progress(Timer, 0, SageDirector.CrossWindup) : 0f;
            ctx.CastGlow = cast;
            ctx.WingSpread = 0.8f * cast;
            ctx.CrestGlow = Math.Max(ctx.CrestGlow, 0.5f + 0.5f * cast);

            if (At(0) && IsServer) {
                ctx.BaseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            }
            for (int k = 0; k < waves; k++) {
                if (At(SageDirector.CrossWindup + k * SageDirector.CrossWaveInterval)) {
                    PlaceWave(ctx, k);
                }
            }
            return Timer >= castEnd + SageDirector.CrossActiveFrames + SageDirector.CrossTail ? EndAttack(ctx) : null;
        }

        private static void PlaceWave(SageStateContext ctx, int wave) {
            Player target = ctx.Target;
            ctx.CrestPulse = 0.7f;
            SageVfx.Cue("bell", target.Center, 1f + 0.12f * wave, 0.8f);
            if (!IsServer) {
                return;
            }
            Vector2 center = target.Center;
            if (wave % 2 == 1) {
                Vector2 lead = target.velocity * SageDirector.CrossTelegraph;
                float length = lead.Length();
                if (length > SageDirector.CrossLeadMax) {
                    lead *= SageDirector.CrossLeadMax / length;
                }
                center += lead;
            }
            float tilt = wave % 2 == 0 ? 0f : MathHelper.PiOver4;
            Shoot<SageCrossBurst>(ctx, center, Vector2.Zero, SageDirector.DmgCross, tilt, SageDirector.CrossTelegraph);
            for (int i = 0; i < SageDirector.CrossSatellites; i++) {
                float bearing = ctx.BaseAngle + wave * MathHelper.PiOver4 + MathHelper.TwoPi * i / SageDirector.CrossSatellites;
                Shoot<SageCrossBurst>(ctx, center + bearing.ToRotationVector2() * SageDirector.CrossSatelliteRadius, Vector2.Zero, SageDirector.DmgCross,
                    tilt + MathHelper.PiOver4, SageDirector.CrossTelegraph);
            }
        }
    }
}