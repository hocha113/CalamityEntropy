using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 绕场直冲:
    /// <list type="bullet">
    /// <item>200 → 141:朝远离玩家的方向拉开到半径 700</item>
    /// <item>140 → 51:沿半径 700 绕着玩家扫过 6~10 rad(方向随机),插值走重复余弦所以中段最快</item>
    /// <item>50:起冲拍,把「穿场中心」记成玩家当前位置并清空尾迹重采</item>
    /// <item>49 → 2:半径从 +700 线性穿到 -700,也就是穿过中心冲到对面;49 → 17 之间开大尾迹</item>
    /// <item>1、0、-1:<b>整个招式体都不跑</b>(外层门是「倒计时大于 1」),速度停在 0、朝向停在最后一帧的值</item>
    /// </list>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.RoundAndDash, typeof(LuminarisStateContext))]
    public class LuminarisRoundAndDashState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.RoundAndDash;

        /// <summary>起冲拍的本地锁存,不过线。它顺手把穿场中心钉在玩家身上,所以慢半拍也要在宽限窗内补上</summary>
        private bool launchCued;

        public override void OnEnter(LuminarisStateContext ctx)
        {
            base.OnEnter(ctx);
            launchCued = false;
        }

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            int c = ctx.Countdown;

            if (c > LuminarisDirector.RoundAndDashBodyGate)
            {
                npc.velocity *= 0;
                npc.rotation = 0;
                ctx.AfterImageTime = LuminarisDirector.AfterImageFrames;

                if (c == LuminarisDirector.RoundAndDashFrames)
                {
                    ctx.Vec1 = npc.Center;
                    ctx.Vec2 = player.Center + (npc.Center - player.Center).SafeNormalize(Vector2.Zero) * LuminarisDirector.RoundAndDashRadius;
                }
                if (c > LuminarisDirector.RoundAndDashSwingFrame && c <= LuminarisDirector.RoundAndDashFrames)
                {
                    npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2,
                        CEUtils.GetRepeatedCosFromZeroToOne(1 - (c - LuminarisDirector.RoundAndDashSwingFrame) / LuminarisDirector.RoundAndDashSwingSpan, 1));
                }
                else
                {
                    if (c == LuminarisDirector.RoundAndDashSwingFrame)
                    {
                        ctx.Num1 = LuminarisDirector.RoundAndDashRadius;
                        ctx.Num2 = (npc.Center - player.Center).ToRotation();
                        if (IsServer)
                        {
                            //绕场跨度与方向只在权威端骰,结果随 Num3 过线。
                            //插值系数在起手几帧内约等于 0,所以客户端等包的那一两帧位置不受影响
                            ctx.Num3 = ctx.Num2 + Main.rand.NextFloat(LuminarisDirector.RoundAndDashSweepMin, LuminarisDirector.RoundAndDashSweepMax) * (Main.rand.NextBool() ? 1 : -1);
                            MarkNetUpdate(ctx);
                        }
                    }
                    if (c < LuminarisDirector.RoundAndDashTrailStartFrame && c > LuminarisDirector.RoundAndDashTrailEndFrame)
                    {
                        ctx.MegaTrail = LuminarisDirector.RoundAndDashTrailStrength;
                    }
                    if (!launchCued && c <= LuminarisDirector.RoundAndDashLaunchFrame)
                    {
                        launchCued = true;
                        if (!CountdownCuePassed(c, LuminarisDirector.RoundAndDashLaunchFrame))
                        {
                            if (!Main.dedServ)
                            {
                                CalamityEntropy.FlashEffectStrength = LuminarisDirector.RoundAndDashLaunchFlash;
                            }
                            CEUtils.PlaySound("flamethrower end", 1, npc.Center);
                            ctx.Vec2 = player.Center;
                            ctx.Trail.Clear();
                            ctx.Trail.Add(npc.Center);
                        }
                    }
                    if (c > LuminarisDirector.RoundAndDashLaunchFrame)
                    {
                        float p = Utils.Remap(c, LuminarisDirector.RoundAndDashSwingFrame, LuminarisDirector.RoundAndDashLaunchFrame, 0, 1);
                        float r = float.Lerp(ctx.Num2, ctx.Num3, CEUtils.GetRepeatedCosFromZeroToOne(p, 1));
                        npc.Center = player.Center + r.ToRotationVector2() * ctx.Num1;
                    }
                    if (c < LuminarisDirector.RoundAndDashLaunchFrame)
                    {
                        //穿场:角固定在绕场终止角,半径从 +700 走到 -700,所以是直线穿过中心
                        float r = ctx.Num3;
                        float p = Utils.Remap(c, LuminarisDirector.RoundAndDashCrossSpanFrame, 0, 0, 1);
                        ctx.Num1 = float.Lerp(LuminarisDirector.RoundAndDashRadius, -LuminarisDirector.RoundAndDashRadius, CEUtils.GetRepeatedCosFromZeroToOne(p, 1));
                        npc.Center = ctx.Vec2 + r.ToRotationVector2() * ctx.Num1;
                    }
                    npc.rotation = (npc.Center - ctx.OldPos).ToRotation() + MathHelper.PiOver2;
                }
            }

            return Tick(ctx, c);
        }
    }
}
