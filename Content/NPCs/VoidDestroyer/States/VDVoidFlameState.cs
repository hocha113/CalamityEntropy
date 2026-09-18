using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 虚空火焰 / 越肩火雨:
    /// P1 hub 闪现到玩家下方 20 格(几何必需)→ 保持相对静止,每 75 帧向下扇形散 9~11 发,弹随即转向上加速;四轮。
    /// P2 起改越肩火雨:hub 闪现到镜头后方 Z -0.4、表观在玩家正下方 360px(从屏幕下缘升起的半透明巨影),
    /// 每轮 9~11 发越肩弹由大缩小、40 帧落到平面上以玩家为心的扇形落点(落点标记提前 30 帧亮),命中后遁入深处;P2 四轮、P3 五轮。
    /// 公平阀:每轮出手前 30 帧锥形火花 + 核心渐亮(预告指哪打哪);越肩弹只在穿过平面那几帧有判定;末轮后 24 帧收招
    /// </summary>
    [VaultState((int)VDStateIndex.VoidFlame, typeof(VDStateContext))]
    public class VDVoidFlameState : VDStateBase
    {
        public override string StateName => "VoidFlame";
        public override VDStateIndex StateIndex => VDStateIndex.VoidFlame;
        /// <summary>压到玩家脚下要穿过玩家,飞过去会撞人;镜头后方更没法飞过去:几何必需闪现</summary>
        public override bool NeedsRepositionBlink => true;
        public override float StartDepth(VDStateContext ctx) => Near(ctx) ? VDDirector.FlameNearDepth : 0f;
        public override Vector2 AnchorFor(VDStateContext ctx)
            => Near(ctx) ? DepthAnchor(ctx, VDDirector.FlameNearApparent, VDDirector.FlameNearDepth) : ctx.Target.Center + VDDirector.FlameOffset;

        private enum Beat { Approach, Fire }
        private Beat beat;
        private int volleys;

        private static bool Near(VDStateContext ctx) => ctx.Phase >= 2;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Approach;
            volleys = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            bool near = Near(ctx);
            if (near) {
                ctx.Depth = VDDirector.FlameNearDepth;
            }
            if (beat == Beat.Approach) {
                Vector2 dest = AnchorFor(ctx);
                DeclareHoverTo(ctx, dest, VDDirector.FlameApproachSpeed, VDDirector.FlameApproachAccel, VDDirector.FlameApproachSlow);
                if (Timer >= VDDirector.FlameApproachFrames || (Timer > 8 && ctx.Npc.Distance(dest) < 80f)) {
                    beat = Beat.Fire;
                    ResetTimer();
                    MarkNetUpdate(ctx);
                }
                return null;
            }

            if (near) {
                DeclareHoldRelativeDepth(ctx, VDDirector.FlameNearApparent, VDDirector.FlameNearDepth, VDDirector.FlameHoldStiffness, VDDirector.FlameHoldLerp, VDDirector.FlameHoldMaxSpeed);
            }
            else {
                DeclareHoldRelative(ctx, VDDirector.FlameOffset, VDDirector.FlameHoldStiffness, VDDirector.FlameHoldLerp, VDDirector.FlameHoldMaxSpeed);
            }
            Vector2 core = ctx.Owner.CorePos;
            int total = VDDirector.FlameVolleys(ctx.Phase);
            //第 k 轮在 Timer = Telegraph + Interval*k 出手,之前 30 帧是锥形预告
            int fireAt = VDDirector.FlameTelegraph + VDDirector.FlameVolleyInterval * volleys;
            if (volleys < total) {
                int untilFire = fireAt - Timer;
                if (untilFire > 0 && untilFire <= VDDirector.FlameTelegraph) {
                    float p = 1f - untilFire / (float)VDDirector.FlameTelegraph;
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, p);
                    ctx.WingPulse = Math.Max(ctx.WingPulse, p * 0.6f);
                    ctx.RimCharge = p;
                    if (untilFire == VDDirector.FlameTelegraph) {
                        VDVfx.Sound("VoidAnticipation", near ? 0.9f : 1.3f, ctx.Owner.ProjectedCorePos, 4, 0.6f);
                    }
                    if (!Main.dedServ && Timer % 2 == 0) {
                        //锥形火花:P1 从核心向下喷;越肩版从投影核心朝玩家方向喷(火雨要落到那边去)
                        Vector2 shown = ctx.Owner.ProjectedCorePos;
                        float baseAng = near ? (ctx.Target.Center - shown).ToRotation() : MathHelper.PiOver2;
                        float sc = VDDepth.Scale(ctx.Owner.Depth);
                        Vector2 v = (baseAng + Main.rand.NextFloat(-VDDirector.FlameSpread, VDDirector.FlameSpread)).ToRotationVector2() * Main.rand.NextFloat(3f, 7f) * (0.5f + p) * sc;
                        VDVfx.SparkBurst(shown, VDVfx.VoidPurple, 1, v.Length(), v.Length(), 14, 0.4f * sc, 0.7f * sc);
                    }
                }
                if (Timer == fireAt) {
                    if (near) {
                        FireNearVolley(ctx, core);
                    }
                    else {
                        FirePlaneVolley(ctx, core);
                    }
                    volleys++;
                }
            }
            else {
                ctx.CoreGlow = 0f;
            }
            if (volleys >= total && Timer >= fireAt - VDDirector.FlameVolleyInterval + VDDirector.FlameTail) {
                return EndAttack(ctx);
            }
            return null;
        }

        /// <summary>P1:向下扇散 9~11 发,弹随即转向上加速</summary>
        private void FirePlaneVolley(VDStateContext ctx, Vector2 core) {
            MuzzleCue(ctx, Vector2.UnitY, 4f, "CruiserSpit", 0.8f);
            if (!IsServer) {
                return;
            }
            ctx.RandCount = Main.rand.Next(VDDirector.FlameCountMin, VDDirector.FlameCountMax);
            for (int i = 0; i < ctx.RandCount; i++) {
                Vector2 vel = (MathHelper.PiOver2 + Main.rand.NextFloat(-VDDirector.FlameSpread, VDDirector.FlameSpread)).ToRotationVector2() * Main.rand.NextFloat(VDDirector.FlameSpeedMin, VDDirector.FlameSpeedMax);
                Shoot<VDVoidBolt>(ctx, core, vel, VDDirector.DmgVoidBolt, VDVoidBolt.ModeFanRise, VDDirector.FlameRiseDelay);
            }
            ctx.Npc.netUpdate = true;
        }

        /// <summary>P2 起:越肩弹从镜头后的核心缩小着落到以玩家为心的扇形落点(张角 ±FlameSpread、半径 120~300),40 帧到平面,再遁入深处</summary>
        private void FireNearVolley(VDStateContext ctx, Vector2 core) {
            Vector2 toTarget = (ctx.Target.Center - core).SafeNormalize(-Vector2.UnitY);
            MuzzleCue(ctx, toTarget, 3f, "CruiserSpit", 0.7f, 1.1f);
            if (!IsServer) {
                return;
            }
            ctx.RandCount = Main.rand.Next(VDDirector.FlameCountMin, VDDirector.FlameCountMax);
            float z = VDDirector.FlameNearDepth;
            for (int i = 0; i < ctx.RandCount; i++) {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 landing = ctx.Target.Center + ang.ToRotationVector2() * Main.rand.NextFloat(VDDirector.FlameNearLandMin, VDDirector.FlameNearLandMax);
                (Vector2 vel, float zVel) = AimThroughPlane(core, z, landing, VDDirector.FlameNearFrames);
                ShootDepth<VDVoidBolt>(ctx, core, vel, VDDirector.DmgVoidBolt, z, zVel, 0f, VDVoidBolt.ModeZFromNear);
            }
            ctx.Npc.netUpdate = true;
        }
    }
}
