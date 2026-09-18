using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 纵深回旋火:斜上方就位(连接段已飞到附近,就位达标即跳拍)→ 30 帧蓄力前摇(汇聚流 + 核心亮 + 翼张 + 起手音)
    /// → 三轮五发:中弹平面直射(可读的基准线),两侧四发抛入深处(顶点 Z 1.6,成远处绕行的小点)再在锁定点上空回头、
    /// 越来越大地穿过平面命中、再掠过镜头,往返 70 帧,回程亮落点标记;每轮前 6 帧核心再亮一次 → 24 帧收招刹停。
    /// 公平阀:第一发伤害前至少 0.5 秒专属预告;回旋弹只在穿过平面那几帧有判定;出手帧 MuzzleCue 反冲
    /// </summary>
    [VaultState((int)VDStateIndex.ArcFireball, typeof(VDStateContext))]
    public class VDArcFireballState : VDStateBase
    {
        public override string StateName => "ArcFireball";
        public override VDStateIndex StateIndex => VDStateIndex.ArcFireball;

        public override Vector2 AnchorFor(VDStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.ArcHoverOffset.X, VDDirector.ArcHoverOffset.Y);

        private enum Beat { Approach, Charge, Fire, Recover }
        private Beat beat;
        private int volleys;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Approach;
            volleys = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            Vector2 dest = AnchorFor(ctx);
            switch (beat) {
                case Beat.Approach:
                    DeclareHoverTo(ctx, dest, VDDirector.ArcApproachSpeed, VDDirector.ArcApproachAccel, VDDirector.ArcApproachSlow);
                    if (Timer >= VDDirector.ArcApproachMax || (Timer > VDDirector.ArcApproachMin && ctx.Npc.Distance(dest) < VDDirector.ArcArriveDist)) {
                        SwitchBeat(Beat.Charge);
                        MarkNetUpdate(ctx);
                    }
                    break;
                case Beat.Charge: {
                        //前摇:核心随进度亮起、翼张、汇聚流越来越密
                        DeclareHoverTo(ctx, dest, VDDirector.ArcHoldSpeed, VDDirector.ArcHoldAccel, VDDirector.ArcHoldSlow);
                        float p = MathHelper.Clamp(Timer / (float)VDDirector.ArcChargeFrames, 0f, 1f);
                        ctx.CoreGlow = Math.Max(ctx.CoreGlow, p);
                        ctx.WingPulse = Math.Max(ctx.WingPulse, p);
                        if (Timer == 1) {
                            VDVfx.Sound("VoidAnticipation", 1.15f, ctx.Owner.CorePos, 3, 0.8f);
                        }
                        if (Timer % (p < 0.5f ? 3 : 2) == 0) {
                            ConvergeSparks(ctx, VDVfx.VoidPurple, 70f, 150f, 0.1f);
                        }
                        if (Timer >= VDDirector.ArcChargeFrames) {
                            SwitchBeat(Beat.Fire);
                        }
                        break;
                    }
                case Beat.Fire: {
                        DeclareHoverTo(ctx, dest, VDDirector.ArcHoldSpeed, VDDirector.ArcHoldAccel, VDDirector.ArcHoldSlow);
                        //第 k 轮在 Timer = 1 + Interval*k 出手,前 6 帧核心再亮一次
                        int fireAt = 1 + VDDirector.ArcVolleyInterval * volleys;
                        int untilFire = fireAt - Timer;
                        if (untilFire > 0 && untilFire <= 6) {
                            ctx.CoreGlow = Math.Max(ctx.CoreGlow, 1f - untilFire / 6f);
                            ConvergeSparks(ctx, VDVfx.VoidPurple, 60f, 120f, 0.12f);
                        }
                        if (Timer == fireAt) {
                            FireVolley(ctx);
                            volleys++;
                        }
                        if (volleys >= VDDirector.ArcVolleys) {
                            SwitchBeat(Beat.Recover);
                        }
                        break;
                    }
                default:
                    //收招:刹停、核心熄灭
                    ctx.CoreGlow = 0f;
                    if (Timer >= VDDirector.ArcTail) {
                        return EndAttack(ctx);
                    }
                    break;
            }
            return null;
        }

        /// <summary>一轮:中弹平面直射;两侧四发沿抛物线抛入深处(顶点 ArcBoomerangApex),回程收敛到锁定点(玩家出手时的位置)</summary>
        private static void FireVolley(VDStateContext ctx) {
            Vector2 core = ctx.Owner.CorePos;
            Vector2 dir = (ctx.Target.Center - core).SafeNormalize(Vector2.UnitY);
            Vector2 lockPos = ctx.Target.Center;
            MuzzleCue(ctx, dir, 3f, "CruiserSpit", 0.9f);
            Shoot<VDVoidBolt>(ctx, core, dir * VDDirector.ArcBoltSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeStraight);
            (float zVel, float zAccel) = VDDepth.Parabola(VDDirector.ArcBoomerangApex, VDDirector.ArcBoomerangFrames);
            foreach (float deg in new[] { -VDDirector.ArcOuterDeg, -VDDirector.ArcInnerDeg, VDDirector.ArcInnerDeg, VDDirector.ArcOuterDeg }) {
                ShootDepth<VDVoidBolt>(ctx, core, dir.RotatedBy(MathHelper.ToRadians(deg)) * VDDirector.ArcBoltSpeed, VDDirector.DmgVoidBolt, 0f, zVel, zAccel, VDVoidBolt.ModeZBoomerang, lockPos.X, lockPos.Y);
            }
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
