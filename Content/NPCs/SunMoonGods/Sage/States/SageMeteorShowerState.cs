using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Sage;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 流星雨,二阶段签名招:本体升到高处展开双翼,成排斜向航线先亮出来,流星沿航线逐道落下,整波像雨幕一样扫过去
    /// 每波换一个倾斜方向;偶数波有一道穿过玩家生成时的位置,奇数波把玩家放在两道正中
    /// </summary>
    [VaultState((int)SageStateIndex.MeteorShower, typeof(SageStateContext))]
    public class SageMeteorShowerState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.MeteorShower;

        public override Vector2 AnchorFor(SageStateContext ctx) => ctx.Target.Center + SageDirector.MeteorAnchor;

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            int waves = SageDirector.MeteorWaves(ctx.Phase);
            int lastWave = SageDirector.MeteorWindup + (waves - 1) * SageDirector.MeteorWaveInterval;
            int castEnd = lastWave + SageDirector.MeteorTelegraph + SageDirector.MeteorLanes * SageDirector.MeteorRipple;

            Hover(ctx, AnchorFor(ctx), SageDirector.HoverSpeed, 0.08f, 160f);
            FaceTo(ctx, MathHelper.PiOver2);
            float cast = Timer < castEnd ? SageDirector.Progress(Timer, 0, SageDirector.MeteorWindup) : 0f;
            ctx.WingSpread = cast;
            ctx.BodyGlow = 0.3f * cast;
            ctx.CastGlow = cast;
            ctx.CrestGlow = Math.Max(ctx.CrestGlow, 0.5f + 0.5f * cast);
            if (Timer < SageDirector.MeteorWindup && Timer % 2 == 0) {
                SageVfx.Converge(ctx.Npc.Center, 120f, 260f);
            }

            if (At(0)) {
                SageVfx.Cue("runesong2", ctx.Npc.Center, 1f, 0.9f);
            }
            for (int k = 0; k < waves; k++) {
                if (At(SageDirector.MeteorWindup + k * SageDirector.MeteorWaveInterval)) {
                    CallWave(ctx, k);
                }
            }
            return Timer >= castEnd + SageDirector.MeteorTail ? EndAttack(ctx) : null;
        }

        private static void CallWave(SageStateContext ctx, int wave) {
            ctx.CrestPulse = 0.8f;
            SageVfx.SkyFlash(0.5f);
            SageVfx.Cue("LunarStar", ctx.Npc.Center, 0.8f, 0.9f);
            if (!IsServer) {
                return;
            }
            int side = (wave % 2 == 0 ? 1 : -1) * ctx.SideDir;
            Vector2 dir = new Vector2(side * MathF.Cos(SageDirector.MeteorAngle), MathF.Sin(SageDirector.MeteorAngle));
            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
            float spacing = SageDirector.MeteorSpacing(ctx.Phase);
            float shift = wave % 2 == 1 ? 0.5f : 0f;
            int half = SageDirector.MeteorLanes / 2;
            Vector2 origin = ctx.Target.Center - dir * SageDirector.MeteorSpawnDistance;
            for (int j = -half; j <= half; j++) {
                //起飞顺着倾斜方向逐道推过去
                int order = side > 0 ? j + half : half - j;
                Shoot<SageMeteor>(ctx, origin + normal * ((j + shift) * spacing), Vector2.Zero, SageDirector.DmgMeteor, dir.ToRotation(),
                    SageDirector.MeteorTelegraph + order * SageDirector.MeteorRipple);
            }
        }
    }
}