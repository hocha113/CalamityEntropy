using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 三角弹绕圈:
    /// <list type="bullet">
    /// <item>200 → 161:入位到玩家侧上方(水平 ±440、上方 440)</item>
    /// <item>160:记下此刻到玩家的距离与方位角,当作后面绕圈的半径与初始角</item>
    /// <item>159 → 148:每帧转 30 度(12 帧正好一圈),逐帧射三角弹(偶数帧蓝、奇数帧红),并把绕圈中心持续钉在玩家身上</item>
    /// <item>147 → -1:每帧转 10 度,绕的是<b>快转段最后记下的那个玩家位置</b>(固定点),约 4.1 圈</item>
    /// </list>
    /// <para>
    /// <c>Vec2</c> 在这一招里被挪用成「上一帧位置」:每帧开头写入当前中心,位置改写之后再用它算朝向。
    /// <c>Num2</c> 是逐帧累加的绕圈角,直接决定位置,所以它必须过线
    /// </para>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.ShootTriangle, typeof(LuminarisStateContext))]
    public class LuminarisShootTriangleState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.ShootTriangle;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;

            npc.velocity *= 0;
            ctx.Vec2 = npc.Center;
            ctx.AfterImageTime = LuminarisDirector.AfterImageFrames;

            if (c == LuminarisDirector.ShootTriangleFrames) {
                if (IsServer) {
                    //原代码在这里骰 num3,但本状态<b>从不读它</b>。骰点与过线照搬
                    ctx.Num3 = Main.rand.NextBool() ? -1 : 1;
                    MarkNetUpdate(ctx);
                }
                ctx.Vec1 = npc.Center;
            }
            if (c > LuminarisDirector.ShootTriangleOrbitFrame) {
                npc.Center = Vector2.Lerp(ctx.Vec1,
                    player.Center + new Vector2(LuminarisDirector.ShootTriangleApproachX * Math.Sign(npc.Center.X - player.Center.X), LuminarisDirector.ShootTriangleApproachY),
                    CEUtils.GetRepeatedCosFromZeroToOne(1 - (c - LuminarisDirector.ShootTriangleOrbitFrame) / LuminarisDirector.ShootTriangleApproachSpan, 1));
                npc.rotation = (npc.Center - ctx.Vec2).ToRotation() + MathHelper.PiOver2;
            }
            if (c == LuminarisDirector.ShootTriangleOrbitFrame) {
                ctx.Num1 = npc.Center.Distance(player.Center);
                ctx.Num2 = (npc.Center - player.Center).ToRotation();
            }
            if (c < LuminarisDirector.ShootTriangleOrbitFrame) {
                if (c >= LuminarisDirector.ShootTriangleFastEndFrame) {
                    ctx.Num2 += MathHelper.ToRadians(LuminarisDirector.ShootTriangleFastStep);
                    npc.Center = player.Center + ctx.Num2.ToRotationVector2() * ctx.Num1;
                    if (IsServer) {
                        //散射方向与速度抖动都吃随机数,只影响弹幕,所以整段收在权威端
                        if (c % 2 == 0) {
                            Shoot<LuminarisTriangleShootBlue>(ctx, npc.Center, TriangleShotVelocity(npc, player, enrange));
                        }
                        else {
                            Shoot<LuminarisTriangleShootRed>(ctx, npc.Center, TriangleShotVelocity(npc, player, enrange));
                        }
                    }
                    npc.rotation = (npc.Center - ctx.Vec2).ToRotation() + MathHelper.PiOver2;
                    //快转段每帧把绕圈中心钉在玩家身上,慢转段接手时用的就是最后这一帧的值
                    ctx.Vec1 = player.Center;
                }
                else {
                    //本状态每帧开头已经把速度清零了,所以这一句阻尼没有效果,照搬
                    npc.velocity *= LuminarisDirector.ShootTriangleSlowDrag;
                    ctx.Num2 += MathHelper.ToRadians(LuminarisDirector.ShootTriangleSlowStep);
                    npc.Center = ctx.Vec1 + ctx.Num2.ToRotationVector2() * ctx.Num1;
                    npc.rotation = (npc.Center - ctx.Vec2).ToRotation() + MathHelper.PiOver2;
                }
            }

            return Tick(ctx, c);
        }

        /// <summary>三角弹初速:朝玩家方向偏 ±36 度,速度 10 × 随机 0.8~1.2 × enrange</summary>
        private static Vector2 TriangleShotVelocity(NPC npc, Player player, float enrange)
            => (player.Center - npc.Center).normalize().RotatedBy(MathHelper.ToRadians(LuminarisDirector.ShootTriangleSpread) * (Main.rand.NextBool() ? 1 : -1))
                * LuminarisDirector.ShootTriangleProjSpeed
                * Main.rand.NextFloat(LuminarisDirector.ShootTriangleSpeedJitterMin, LuminarisDirector.ShootTriangleSpeedJitterMax) * enrange;
    }
}
