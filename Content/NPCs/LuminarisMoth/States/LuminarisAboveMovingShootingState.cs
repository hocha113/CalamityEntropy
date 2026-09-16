using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using InnoVault.PRT;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 高空横移弹雨。倒计时四段:
    /// <list type="number">
    /// <item>290 → 261:完全悬停,什么都不做(位置只被每帧的 <c>velocity *= 0</c> 冻住)</item>
    /// <item>260 → 241:入位到玩家侧上方(水平 ±360、上方 440)</item>
    /// <item>240 → 221:挪到玩家正上方 420</item>
    /// <item>220:落地拍(屏震 + 闪屏 + 14 发散射);219 → -1:余弦横移并从场地两侧甩横扫星弹</item>
    /// </list>
    /// 220 那一帧三条位置分支都不命中(两段入位都要求 &gt; 220、横移要求 &lt; 220),所以落地拍是一个静止帧
    /// </summary>
    [VaultState((int)LuminarisStateIndex.AboveMovingShooting, typeof(LuminarisStateContext))]
    public class LuminarisAboveMovingShootingState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.AboveMovingShooting;

        /// <summary>落地拍的本地锁存,不过线。慢半拍的客户端在宽限窗内仍补这一拍,越过就静默(那是真的中途加入)</summary>
        private bool slamCued;

        public override void OnEnter(LuminarisStateContext ctx)
        {
            base.OnEnter(ctx);
            slamCued = false;
        }

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;

            npc.rotation = 0;
            npc.velocity *= 0;
            ctx.AfterImageTime = LuminarisDirector.AfterImageFrames;

            if (c <= LuminarisDirector.AboveMovingGateFrame)
            {
                if (c > LuminarisDirector.AboveMovingStage1Frame)
                {
                    if (c == LuminarisDirector.AboveMovingGateFrame)
                    {
                        if (IsServer)
                        {
                            //原代码在这里骰 num3,但本状态<b>从不读它</b>。骰点与过线照搬,免得动到随机序列语义
                            ctx.Num3 = Main.rand.NextBool() ? -1 : 1;
                            MarkNetUpdate(ctx);
                        }
                        ctx.Vec1 = npc.Center;
                    }
                    npc.Center = Vector2.Lerp(ctx.Vec1,
                        player.Center + new Vector2(LuminarisDirector.AboveMovingStage1X * Math.Sign(npc.Center.X - player.Center.X), LuminarisDirector.AboveMovingStage1Y),
                        CEUtils.GetRepeatedCosFromZeroToOne(1 - (c - LuminarisDirector.AboveMovingStage1Frame) / LuminarisDirector.AboveMovingStage1Span, 1));
                }
                else if (c > LuminarisDirector.AboveMovingStage2Frame)
                {
                    //240 落在这一支(上一支要求严格大于 240),所以这里的锚点重置确实会执行
                    if (c == LuminarisDirector.AboveMovingStage1Frame)
                    {
                        ctx.Vec1 = npc.Center;
                    }
                    npc.Center = Vector2.Lerp(ctx.Vec1,
                        player.Center + new Vector2(0, LuminarisDirector.AboveMovingStage2Y),
                        CEUtils.GetRepeatedCosFromZeroToOne(1 - (c - LuminarisDirector.AboveMovingStage2Frame) / LuminarisDirector.AboveMovingStage2Span, 1));
                }
                if (c < LuminarisDirector.AboveMovingStage2Frame)
                {
                    //横移幅度渐入:219 → 160 线性涨满,之后恒为 1
                    float f = c < LuminarisDirector.AboveMovingRampFrame ? 1 : 1 - ((c - LuminarisDirector.AboveMovingRampFrame) / LuminarisDirector.AboveMovingRampSpan);
                    npc.Center = new Vector2(
                        player.Center.X + (float)Math.Cos(c * LuminarisDirector.AboveMovingSwayFreq) * LuminarisDirector.AboveMovingSwayAmp * f,
                        npc.Center.Y + ((player.Center.Y - LuminarisDirector.AboveMovingHoverAbove) - npc.Center.Y) * LuminarisDirector.AboveMovingHoverLerp);
                    //横移段的朝向直接拿本帧位移的 X 分量当倾角,纯绘制
                    npc.rotation = (npc.Center - ctx.OldPos).X * LuminarisDirector.AboveMovingTiltFactor;
                    if (c < LuminarisDirector.AboveMovingShootStartFrame)
                    {
                        int interval = (int)(LuminarisDirector.AboveMovingShootIntervalBase / enrange);
                        if (c % interval == 0)
                        {
                            //左右两道横扫 + 本体正下方一发,重力载荷走 ai0 方向 / ai1 强度 / ai2 延迟
                            Shoot<LuminarisAstralShoot>(ctx, player.Center + new Vector2(-LuminarisDirector.AboveMovingSideOffsetX, LuminarisDirector.AboveMovingSideOffsetY),
                                Vector2.UnitX * LuminarisDirector.AboveMovingSideSpeed * enrange, 1,
                                Vector2.UnitY.ToRotation(), LuminarisDirector.AboveMovingSideGravity * enrange, LuminarisDirector.AboveMovingGravityDelay);
                            Shoot<LuminarisAstralShoot>(ctx, player.Center + new Vector2(LuminarisDirector.AboveMovingSideOffsetX, LuminarisDirector.AboveMovingSideOffsetY),
                                Vector2.UnitX * -LuminarisDirector.AboveMovingSideSpeed * enrange, 1,
                                Vector2.UnitY.ToRotation(), LuminarisDirector.AboveMovingSideGravity * enrange, LuminarisDirector.AboveMovingGravityDelay);
                            Shoot<LuminarisAstralShoot>(ctx, npc.Center,
                                Vector2.UnitY * LuminarisDirector.AboveMovingDownSpeed * enrange, 1,
                                (-Vector2.UnitY).ToRotation(), LuminarisDirector.AboveMovingDownGravity * enrange, LuminarisDirector.AboveMovingGravityDelay);
                            if (!Main.dedServ)
                            {
                                //竖直天顶线预告:横坐标用「上一次开火那一帧」的余弦相位。
                                //原代码写成 `(int)(8f / enrange) * 1`,那个 × 1 是无作用的冗余,原样留着
                                PRTLoader.NewParticle<PRT_HadLine>(
                                    player.Center + player.velocity + new Vector2((float)Math.Cos((c - interval * 1) * LuminarisDirector.AboveMovingSwayFreq) * LuminarisDirector.AboveMovingSwayAmp, LuminarisDirector.AboveMovingPreviewOffsetY),
                                    Vector2.Zero, Color.LightBlue * LuminarisDirector.AboveMovingPreviewAlpha, 1)
                                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, MathHelper.PiOver2);
                            }
                        }
                    }
                }
                if (!slamCued && c <= LuminarisDirector.AboveMovingStage2Frame)
                {
                    slamCued = true;
                    if (!CountdownCuePassed(c, LuminarisDirector.AboveMovingStage2Frame))
                    {
                        if (!Main.dedServ)
                        {
                            ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero,
                                Utils.Remap(Main.LocalPlayer.Distance(npc.Center), LuminarisDirector.AboveMovingSlamShakeFar, LuminarisDirector.AboveMovingSlamShakeNear, 0f, LuminarisDirector.AboveMovingSlamShakeAmp)));
                            CalamityEntropy.FlashEffectStrength = LuminarisDirector.AboveMovingSlamFlash;
                        }
                        CEUtils.PlaySound("ksLand", LuminarisDirector.AboveMovingSlamPitch, npc.Center);
                        if (IsServer)
                        {
                            //散射的方向与偏转都吃随机数,只影响弹幕,所以整段收在权威端
                            for (int i = 0; i < LuminarisDirector.AboveMovingSlamShots; i++)
                            {
                                Shoot<LuminarisAstralShoot>(ctx, npc.Center,
                                    new Vector2(LuminarisDirector.AboveMovingSlamSpeedX * (Main.rand.NextBool() ? 1 : -1), LuminarisDirector.AboveMovingSlamSpeedY).RotatedByRandom(LuminarisDirector.AboveMovingSlamScatter) * enrange,
                                    1, Vector2.UnitY.ToRotation(), LuminarisDirector.AboveMovingSlamGravity * enrange, LuminarisDirector.AboveMovingSlamGravityDelay);
                            }
                        }
                    }
                }
            }

            return Tick(ctx, c);
        }
    }
}
