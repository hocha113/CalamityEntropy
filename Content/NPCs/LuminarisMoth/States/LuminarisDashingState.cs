using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 冲撞:锁向 → 直冲 → 再锁向 → 再直冲。
    /// <list type="bullet">
    /// <item>200 → 180:把朝向从当时的值转到「指向玩家」,插值系数走重复余弦(先慢后快再慢)</item>
    /// <item>179 → 131:以 40 速沿朝向直冲,并带极慢的持续追瞄</item>
    /// <item>130 → 110:重新锁向,这一次插值系数是<b>线性</b>的(和第一次不同)。速度不改写,所以边转边飞</item>
    /// <item>109 → -1:再一次直冲</item>
    /// </list>
    /// <para>
    /// 200 → 180 这一段也不改写速度,所以本体带着上一手的残余速度在转向。
    /// <b>这是全场唯一让朝向反过来驱动速度的状态</b>,所以 <c>NPC.rotation</c> 必须随 ExtraAI 过线
    /// </para>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.Dashing, typeof(LuminarisStateContext))]
    public class LuminarisDashingState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.Dashing;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;

            ctx.AfterImageTime = LuminarisDirector.AfterImageFrames;

            if (c == LuminarisDirector.DashingFrames)
            {
                ctx.Num1 = npc.rotation;
                ctx.Num2 = (player.Center - npc.Center).ToRotation() + MathHelper.PiOver2;
            }
            if (c <= LuminarisDirector.DashingFrames && c >= LuminarisDirector.DashingLock1EndFrame)
            {
                npc.rotation = CEUtils.RotateTowardsAngle(ctx.Num1, ctx.Num2,
                    CEUtils.GetRepeatedCosFromZeroToOne(Utils.Remap(c, LuminarisDirector.DashingFrames, LuminarisDirector.DashingLock1EndFrame, 0, 1), 1), false);
            }
            if (c == LuminarisDirector.DashingLock2StartFrame)
            {
                ctx.Num1 = npc.rotation;
                ctx.Num2 = (player.Center - npc.Center).ToRotation() + MathHelper.PiOver2;
            }
            if (c <= LuminarisDirector.DashingLock2StartFrame && c >= LuminarisDirector.DashingLock2EndFrame)
            {
                npc.rotation = CEUtils.RotateTowardsAngle(ctx.Num1, ctx.Num2,
                    Utils.Remap(c, LuminarisDirector.DashingLock2StartFrame, LuminarisDirector.DashingLock2EndFrame, 0, 1), false);
            }
            if (c < LuminarisDirector.DashingLock2EndFrame
                || (c > LuminarisDirector.DashingLock2StartFrame && c < LuminarisDirector.DashingLock1EndFrame))
            {
                npc.velocity = (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * LuminarisDirector.DashingSpeed;
                //追瞄速率是 `0.25f * enrange.ToRadians()`:ToRadians 把 enrange 本身当角度换算,
                //所以实际只有 0.004~0.010 rad/帧。看着像笔误,但这是原代码的写法
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation,
                    (player.Center - npc.Center).ToRotation() + MathHelper.PiOver2,
                    LuminarisDirector.DashingTrackRate * enrange.ToRadians(), true);
            }

            return Tick(ctx, c);
        }
    }
}
