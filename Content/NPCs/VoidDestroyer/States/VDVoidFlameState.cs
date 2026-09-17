using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 虚空火焰:hub 闪现到玩家下方 20 格(几何必需)→ 保持相对静止,每 75 帧向下扇形散 9~11 发,弹随即转向上加速;
    /// P1/P2 四轮、P3 五轮。公平阀:每轮出手前 30 帧向下喷锥形火花 + 核心渐亮(预告指哪打哪);末轮后 24 帧收招
    /// </summary>
    [VaultState((int)VDStateIndex.VoidFlame, typeof(VDStateContext))]
    public class VDVoidFlameState : VDStateBase
    {
        public override string StateName => "VoidFlame";
        public override VDStateIndex StateIndex => VDStateIndex.VoidFlame;
        /// <summary>压到玩家脚下要穿过玩家,飞过去会撞人:几何必需闪现</summary>
        public override bool NeedsRepositionBlink => true;
        public override Vector2 AnchorFor(VDStateContext ctx) => ctx.Target.Center + VDDirector.FlameOffset;

        private enum Beat { Approach, Fire }
        private Beat beat;
        private int volleys;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Approach;
            volleys = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            if (beat == Beat.Approach) {
                DeclareHoverTo(ctx, ctx.Target.Center + VDDirector.FlameOffset, VDDirector.FlameApproachSpeed, VDDirector.FlameApproachAccel, VDDirector.FlameApproachSlow);
                if (Timer >= VDDirector.FlameApproachFrames || (Timer > 8 && ctx.Npc.Distance(ctx.Target.Center + VDDirector.FlameOffset) < 80f)) {
                    beat = Beat.Fire;
                    ResetTimer();
                    MarkNetUpdate(ctx);
                }
                return null;
            }

            DeclareHoldRelative(ctx, VDDirector.FlameOffset, VDDirector.FlameHoldStiffness, VDDirector.FlameHoldLerp, VDDirector.FlameHoldMaxSpeed);
            Vector2 core = ctx.Owner.CorePos;
            int total = VDDirector.FlameVolleys(ctx.Phase);
            //第 k 轮在 Timer = Telegraph + Interval*k 出手,之前 30 帧是向下锥形预告
            int fireAt = VDDirector.FlameTelegraph + VDDirector.FlameVolleyInterval * volleys;
            if (volleys < total) {
                int untilFire = fireAt - Timer;
                if (untilFire > 0 && untilFire <= VDDirector.FlameTelegraph) {
                    float p = 1f - untilFire / (float)VDDirector.FlameTelegraph;
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, p);
                    ctx.WingPulse = Math.Max(ctx.WingPulse, p * 0.6f);
                    if (untilFire == VDDirector.FlameTelegraph) {
                        VDVfx.Sound("VoidAnticipation", 1.3f, core, 4, 0.6f);
                    }
                    if (!Main.dedServ && Timer % 2 == 0) {
                        Vector2 v = (MathHelper.PiOver2 + Main.rand.NextFloat(-VDDirector.FlameSpread, VDDirector.FlameSpread)).ToRotationVector2() * Main.rand.NextFloat(3f, 7f) * (0.5f + p);
                        VDVfx.SparkBurst(core, VDVfx.VoidPurple, 1, v.Length(), v.Length(), 14, 0.4f, 0.7f);
                    }
                }
                if (Timer == fireAt) {
                    MuzzleCue(ctx, Vector2.UnitY, 4f, "CruiserSpit", 0.8f);
                    if (IsServer) {
                        ctx.RandCount = Main.rand.Next(VDDirector.FlameCountMin, VDDirector.FlameCountMax);
                        for (int i = 0; i < ctx.RandCount; i++) {
                            Vector2 vel = (MathHelper.PiOver2 + Main.rand.NextFloat(-VDDirector.FlameSpread, VDDirector.FlameSpread)).ToRotationVector2() * Main.rand.NextFloat(VDDirector.FlameSpeedMin, VDDirector.FlameSpeedMax);
                            Shoot<VDVoidBolt>(ctx, core, vel, VDDirector.DmgVoidBolt, VDVoidBolt.ModeFanRise, VDDirector.FlameRiseDelay);
                        }
                        ctx.Npc.netUpdate = true;
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
    }
}
