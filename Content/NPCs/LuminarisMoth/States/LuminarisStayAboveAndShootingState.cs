using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 悬停环射:每帧朝「玩家侧上方」这个锚点加速 1 再阻尼 0.97,黏着不放;
    /// 开火窗内每 <c>(int)(50 / enrange)</c> 帧后坐一下并打出一圈星弹。
    /// <para>
    /// 锚点写法是 <c>player.Center + new Vector2(600 × sign, -400) / enrange</c>——
    /// 除法按优先级只作用在偏移向量上,所以 enrange 越高本体贴得越近
    /// </para>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.StayAboveAndShooting, typeof(LuminarisStateContext))]
    public class LuminarisStayAboveAndShootingState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.StayAboveAndShooting;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;

            npc.rotation = 0;
            npc.velocity += (player.Center + new Vector2(LuminarisDirector.StayAboveAnchorX * Math.Sign(npc.Center.X - player.Center.X), LuminarisDirector.StayAboveAnchorY) / enrange - npc.Center).normalize() * LuminarisDirector.StayAboveAccel;
            npc.velocity *= LuminarisDirector.StayAboveDrag;

            //四个判定的合取,原代码就是这么并排写的:低门槛除以 enrange、高门槛乘 enrange,再加一条固定的 > 30
            if (c > LuminarisDirector.StayAboveShootLowGate / enrange
                && c % (int)(LuminarisDirector.StayAboveShootIntervalBase / enrange) == 0
                && c < LuminarisDirector.StayAboveShootHighGate * enrange
                && c > LuminarisDirector.StayAboveShootFloor) {
                //后坐是运动,所以各端都要跑:不能跟着下面的弹幕一起进权威端门
                npc.velocity -= (player.Center - npc.Center).normalize() * LuminarisDirector.StayAboveRecoil;
                int mxr = (int)(LuminarisDirector.StayAboveShotsPerEnrange * enrange);
                float a = 0;
                float rj = LuminarisDirector.StayAboveAngleTotal / mxr;
                for (int i = 0; i < mxr; i++) {
                    //a 是「度」,却被塞进按弧度解释的 ai0(弹幕拿它当重力方向),
                    //所以实际重力方向是 0、36、72… 弧度而不是均分一圈。原代码如此,照搬
                    Shoot<LuminarisAstralShoot>(ctx, npc.Center,
                        (player.Center - npc.Center).normalize() * LuminarisDirector.StayAboveProjSpeed * enrange,
                        LuminarisDirector.StayAboveDamageMult, a,
                        LuminarisDirector.StayAboveProjGravity * enrange, LuminarisDirector.StayAboveProjGravityDelay * enrange);
                    a += rj;
                }
                if (!Main.dedServ) {
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero,
                        Utils.Remap(Main.LocalPlayer.Distance(npc.Center), LuminarisDirector.StayAboveShakeFar, LuminarisDirector.StayAboveShakeNear, 0f, LuminarisDirector.StayAboveShakeAmp)));
                }
                CEUtils.PlaySound("ksLand", LuminarisDirector.StayAbovePitch, npc.Center, volume: LuminarisDirector.StayAboveVolume);
            }

            return Tick(ctx, c);
        }
    }
}
