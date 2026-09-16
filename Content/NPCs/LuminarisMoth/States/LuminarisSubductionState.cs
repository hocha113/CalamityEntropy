using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 俯冲。两轮「抬升 → 拱形俯冲」之后转为直接追撞:
    /// <list type="bullet">
    /// <item>230 → 191:抬到玩家侧上方(水平 ±380 × enrange、上方 400),落点每帧重算</item>
    /// <item>190 → 160:横穿到玩家另一侧,同时叠一条 400 高的余弦拱形(先下沉再抬回)</item>
    /// <item>159 → 151:<b>没有任何分支接管位置</b>,本体靠残余速度滑行 9 帧</item>
    /// <item>150 → 131 / 130 → 90:第二轮抬升与俯冲,跨度分别是 20 与 40 帧</item>
    /// <item>89 → -1:每帧朝玩家加速 0.6 直接追撞</item>
    /// </list>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.Subduction, typeof(LuminarisStateContext))]
    public class LuminarisSubductionState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.Subduction;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;

            ctx.AfterImageTime = LuminarisDirector.AfterImageFrames;

            if (c == LuminarisDirector.SubductionFrames) {
                ctx.Vec1 = npc.Center;
                ctx.Vec2 = RisePoint(npc, player, enrange);
            }
            if (c > LuminarisDirector.SubductionRise1EndFrame) {
                //落点每帧重算,所以抬升途中玩家跑动会把目标点一起带走
                ctx.Vec2 = RisePoint(npc, player, enrange);
                npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2,
                    CEUtils.GetRepeatedCosFromZeroToOne(Utils.Remap(c, LuminarisDirector.SubductionFrames, LuminarisDirector.SubductionRise1EndFrame, 0, 1), 1));
            }
            if (c == LuminarisDirector.SubductionRise1EndFrame) {
                ctx.Vec1 = npc.Center;
                ctx.Vec2 = MirrorPoint(npc, player);
            }
            npc.rotation = 0;
            if (c <= LuminarisDirector.SubductionRise1EndFrame && c >= LuminarisDirector.SubductionDive1EndFrame) {
                float p = 1 - (c - (float)LuminarisDirector.SubductionDive1EndFrame) / LuminarisDirector.SubductionDive1Span;
                npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2, CEUtils.GetRepeatedCosFromZeroToOne(p, 1)) + new Vector2(0, ArcOffset(p));
                npc.rotation = (npc.Center - ctx.OldPos).ToRotation() + MathHelper.PiOver2;
            }

            if (c <= LuminarisDirector.SubductionRound2Frame) {
                if (c == LuminarisDirector.SubductionRound2Frame) {
                    ctx.Vec1 = npc.Center;
                    ctx.Vec2 = RisePoint(npc, player, enrange);
                }
                if (c > LuminarisDirector.SubductionRise2EndFrame) {
                    ctx.Vec2 = RisePoint(npc, player, enrange);
                    npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2,
                        CEUtils.GetRepeatedCosFromZeroToOne(Utils.Remap(c, LuminarisDirector.SubductionRound2Frame, LuminarisDirector.SubductionRise2EndFrame, 0, 1), 1));
                }
                if (c == LuminarisDirector.SubductionRise2EndFrame) {
                    ctx.Vec1 = npc.Center;
                    ctx.Vec2 = MirrorPoint(npc, player);
                }
                if (c < LuminarisDirector.SubductionRise2EndFrame && c >= LuminarisDirector.SubductionDive2EndFrame) {
                    float p = 1 - (c - (float)LuminarisDirector.SubductionDive2EndFrame) / LuminarisDirector.SubductionDive2Span;
                    npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2, CEUtils.GetRepeatedCosFromZeroToOne(p, 1)) + new Vector2(0, ArcOffset(p));
                    npc.rotation = (npc.Center - ctx.OldPos).ToRotation() + MathHelper.PiOver2;
                }
            }
            if (c < LuminarisDirector.SubductionDive2EndFrame) {
                npc.velocity += (player.Center - npc.Center).SafeNormalize(Vector2.Zero) * LuminarisDirector.SubductionChaseAccel;
                npc.velocity *= LuminarisDirector.SubductionChaseDrag;
                npc.rotation = npc.velocity.X * LuminarisDirector.SubductionTiltFactor;
            }
            if (c == LuminarisDirector.SubductionBrakeFrame) {
                //这一拍<b>没有效果</b>:同一帧上面的追撞分支已经先写过速度,而 0 与 -1 两帧追撞又会继续加速。
                //原代码如此,照搬
                npc.velocity *= 0;
                npc.rotation = 0;
            }

            return Tick(ctx, c);
        }

        /// <summary>抬升落点:玩家水平方向 ±380 × enrange、上方 400</summary>
        private static Vector2 RisePoint(NPC npc, Player player, float enrange)
            => player.Center + new Vector2(Math.Sign(npc.Center.X - player.Center.X) * LuminarisDirector.SubductionRiseX * enrange, LuminarisDirector.SubductionRiseY);

        /// <summary>
        /// 俯冲落点:横坐标是本体相对玩家的镜像,纵坐标保持当前高度。
        /// 原代码写成 <c>player.Center - new Vector2(Center.X - player.Center.X, 0)</c> 再单独盖掉 Y
        /// </summary>
        private static Vector2 MirrorPoint(NPC npc, Player player) {
            Vector2 target = player.Center - new Vector2(npc.Center.X - player.Center.X, 0);
            target.Y = npc.Center.Y;
            return target;
        }

        /// <summary>俯冲途中叠加的拱形:进度 0 与 1 处为 0,中途最高 400</summary>
        private static float ArcOffset(float p)
            => (float)(Math.Cos(MathHelper.TwoPi * p - MathHelper.Pi) * 0.5f + 0.5f) * LuminarisDirector.SubductionArcHeight;
    }
}
