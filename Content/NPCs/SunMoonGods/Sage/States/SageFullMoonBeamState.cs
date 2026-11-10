using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Sage;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 满月光炮,三阶段签名招:本体停住蓄力,预告画出起始线和整片扫掠扇面,巨型光柱点亮后匀速扫过扇面;共两次,方向相反
    /// 起始线落在玩家方位的扫掠反方向一侧,玩家可以顺着扫掠方向跑出扇面,也可以在预告期间穿到起始线背后
    /// 光柱自己算角度和判定,状态只管生成、姿态和让头跟着转
    /// </summary>
    [VaultState((int)SageStateIndex.FullMoonBeam, typeof(SageStateContext))]
    public class SageFullMoonBeamState : SageStateBase
    {
        /// <summary>起手先飞这么多帧到位,之后整招不再移动</summary>
        private const int SettleFrames = 10;

        public override SageStateIndex StateIndex => SageStateIndex.FullMoonBeam;

        public override Vector2 AnchorFor(SageStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SageDirector.BeamAnchor.X, SageDirector.BeamAnchor.Y);

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            int since = Timer - SettleFrames;
            if (since < 0) {
                Hover(ctx, AnchorFor(ctx), SageDirector.HoverSpeed, 0.1f, 140f);
                ctx.WingSpread = 0.5f;
                return null;
            }
            Direct(ctx);
            npc.velocity *= 0.8f;

            int sweep = Math.Min(since / SageDirector.BeamCycle, SageDirector.BeamSweeps - 1);
            int cycleStart = SettleFrames + sweep * SageDirector.BeamCycle;
            int local = Timer - cycleStart;
            int sweepDir = (sweep % 2 == 0 ? 1 : -1) * ctx.SideDir;

            if (At(cycleStart)) {
                if (IsServer) {
                    float bearing = (ctx.Target.Center - npc.Center).ToRotation();
                    ctx.LockedAngle = bearing - sweepDir * SageDirector.BeamStartOffset;
                    MarkNetUpdate(ctx);
                    Vector2 muzzle = npc.Center + ctx.LockedAngle.ToRotationVector2() * SageDirector.MuzzleDistance;
                    Shoot<SageMoonBeam>(ctx, muzzle, Vector2.Zero, SageDirector.DmgBeam, ctx.LockedAngle, npc.whoAmI, sweepDir);
                }
                SageVfx.Cue("bell", npc.Center, 0.7f);
            }

            if (local < SageDirector.BeamLife) {
                //光柱晚一帧开始计龄
                FaceTo(ctx, SageMoonBeam.AngleAt(ctx.LockedAngle, sweepDir, local + 1));
                float charge = SageDirector.Progress(local, 0, SageDirector.BeamTelegraph);
                bool firing = local >= SageDirector.BeamTelegraph && local < SageDirector.BeamLife - SageDirector.BeamFadeFrames;
                ctx.CastGlow = firing ? 1f : charge;
                ctx.BodyGlow = firing ? 0.5f : 0.35f * charge;
                ctx.WingSpread = firing ? -0.4f : charge;
                ctx.CrestGlow = 1f;
                ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, firing ? 0.35f : 0.3f * charge);
                if (!firing && local < SageDirector.BeamTelegraph) {
                    SageVfx.Converge(npc.Center + ctx.LockedAngle.ToRotationVector2() * SageDirector.MuzzleDistance, 80f, 220f);
                }
            }
            else {
                FaceTo(ctx, npc.rotation);
            }

            int end = SettleFrames + SageDirector.BeamSweeps * SageDirector.BeamCycle - SageDirector.BeamGap + SageDirector.BeamTail;
            return Timer >= end ? EndAttack(ctx) : null;
        }
    }
}