using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 弧形火球:斜上方快速逼近(就位达标即跳拍)→ 三轮五发扇形:中间直射,两侧四发弧形包裹玩家。
    /// 公平阀:每轮出手前 5 帧核心先亮(预告),出手帧 MuzzleCue 反冲;末轮后只留 20 帧收尾
    /// </summary>
    [VaultState((int)VDStateIndex.ArcFireball, typeof(VDStateContext))]
    public class VDArcFireballState : VDStateBase
    {
        public override string StateName => "ArcFireball";
        public override VDStateIndex StateIndex => VDStateIndex.ArcFireball;
        public override bool NeedsRepositionBlink => true;

        private enum Beat { Approach, Fire }
        private Beat beat;
        private int volleys;

        public override void OnEnter(VDStateContext ctx)
        {
            base.OnEnter(ctx);
            beat = Beat.Approach;
            volleys = 0;
        }

        public override IVDState OnUpdate(VDStateContext ctx)
        {
            Timer++;
            Vector2 dest = ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.ArcHoverOffset.X, VDDirector.ArcHoverOffset.Y);

            if (beat == Beat.Approach)
            {
                DeclareHoverTo(ctx, dest, VDDirector.ArcApproachSpeed, VDDirector.ArcApproachAccel, VDDirector.ArcApproachSlow);
                bool arrived = Timer > VDDirector.ArcApproachMin && ctx.Npc.Distance(dest) < VDDirector.ArcArriveDist;
                if (Timer >= VDDirector.ArcApproachMax || arrived)
                {
                    beat = Beat.Fire;
                    ResetTimer();
                    MarkNetUpdate(ctx);
                }
                return null;
            }

            DeclareHoverTo(ctx, dest, VDDirector.ArcHoldSpeed, VDDirector.ArcHoldAccel, VDDirector.ArcHoldSlow);
            //第 k 轮在 Timer = Lead + Interval*k 出手,前 5 帧核心预亮 + 汇聚火花:预告
            const int lead = 6;
            int fireAt = lead + VDDirector.ArcVolleyInterval * volleys;
            int untilFire = fireAt - Timer;
            if (volleys < VDDirector.ArcVolleys && untilFire > 0 && untilFire <= 5)
            {
                ctx.CoreGlow = Math.Max(ctx.CoreGlow, 1f - untilFire / 5f);
                ConvergeSparks(ctx, VDVfx.VoidPurple, 60f, 120f, 0.12f);
            }
            if (Timer == fireAt && volleys < VDDirector.ArcVolleys)
            {
                Vector2 core = ctx.Owner.CorePos;
                Vector2 dir = (ctx.Target.Center - core).SafeNormalize(Vector2.UnitY);
                Vector2 lockPos = ctx.Target.Center;
                MuzzleCue(ctx, dir, 3f, "CruiserSpit", 0.9f);
                Shoot<VDVoidBolt>(ctx, core, dir * VDDirector.ArcBoltSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeStraight);
                foreach (float deg in new[] { -VDDirector.ArcOuterDeg, -VDDirector.ArcInnerDeg, VDDirector.ArcInnerDeg, VDDirector.ArcOuterDeg })
                {
                    Shoot<VDVoidBolt>(ctx, core, dir.RotatedBy(MathHelper.ToRadians(deg)) * VDDirector.ArcBoltSpeed, VDDirector.DmgVoidBolt, VDVoidBolt.ModeArcWrap, lockPos.X, lockPos.Y);
                }
                volleys++;
            }
            if (volleys >= VDDirector.ArcVolleys && Timer >= fireAt - VDDirector.ArcVolleyInterval + VDDirector.ArcTail)
            {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
