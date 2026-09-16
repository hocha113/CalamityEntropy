using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 定点绕转喷涡:倒计时 200 → 161 入位到玩家侧上方,160 定下半径与初始角,
    /// 之后绕着玩家匀速转圈,每 <c>(int)(38 / enrange)</c> 帧朝玩家喷一发漩涡。
    /// <para>
    /// 也是本 Boss 的<b>初始状态</b>:生成后倒计时的初值(非天顶 0、天顶分身 210~270)
    /// 不经选招直接被这一招读走,所以 <c>OnEnter</c> 不许碰倒计时。
    /// 非天顶那一路初值 0 会让第一帧读到 -1、直接落进「绕转」分支,而此时半径与角都还是 0,
    /// 于是本体被硬写到玩家身上一帧——原代码就是这样,照搬。
    /// </para>
    /// <para>这一招不造成接触伤害(见宿主 <c>CanHitPlayer</c>)。</para>
    /// </summary>
    [VaultState((int)LuminarisStateIndex.RoundShooting, typeof(LuminarisStateContext))]
    public class LuminarisRoundShootingState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.RoundShooting;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            float enrange = ctx.Enrange;
            int c = ctx.Countdown;

            npc.velocity *= 0;
            npc.rotation = 0;
            ctx.AfterImageTime = LuminarisDirector.AfterImageFrames;

            if (c == LuminarisDirector.RoundShootingFrames)
            {
                if (IsServer)
                {
                    //绕转方向只在权威端骰,结果随 Num3 过线。客户端拿到之前 Num3 是 0,不会自己转
                    ctx.Num3 = Main.rand.NextBool() ? -1 : 1;
                    MarkNetUpdate(ctx);
                }
                ctx.Vec1 = npc.Center;
            }
            if (c > LuminarisDirector.RoundShootingOrbitFrame && c < LuminarisDirector.RoundShootingFrames)
            {
                //落点每帧按当前左右侧重算,所以入位途中越过玩家会把目标点翻到另一侧
                npc.Center = Vector2.Lerp(ctx.Vec1,
                    player.Center + new Vector2(LuminarisDirector.RoundShootingApproachX * Math.Sign(npc.Center.X - player.Center.X), LuminarisDirector.RoundShootingApproachY),
                    CEUtils.GetRepeatedCosFromZeroToOne(1 - (c - LuminarisDirector.RoundShootingOrbitFrame) / LuminarisDirector.RoundShootingApproachSpan, 1));
            }
            if (c == LuminarisDirector.RoundShootingOrbitFrame)
            {
                ctx.Num1 = npc.Center.Distance(player.Center);
                ctx.Num2 = (npc.Center - player.Center).ToRotation();
            }
            if (c < LuminarisDirector.RoundShootingOrbitFrame)
            {
                //整条尾迹跟着玩家平移,纯绘制处理:绕转时本体贴着玩家动,尾迹不跟就会被甩在世界坐标上
                for (int i = 0; i < ctx.Trail.Count; i++)
                {
                    ctx.Trail[i] += player.velocity;
                }
                ctx.Num2 += LuminarisDirector.RoundShootingOrbitSpeed * ctx.Num3 * enrange;
                npc.Center = player.Center + ctx.Num2.ToRotationVector2() * ctx.Num1;
                if (c % (int)(LuminarisDirector.RoundShootingShootIntervalBase / enrange) == 0)
                {
                    CEUtils.PlaySound("bne_hit2", 1, npc.Center);
                    Shoot<LuminarisVortex>(ctx, npc.Center, (player.Center - npc.Center).normalize() * LuminarisDirector.RoundShootingVortexSpeed);
                }
                npc.rotation = (npc.Center - ctx.OldPos).ToRotation() + MathHelper.PiOver2;
            }

            return Tick(ctx, c);
        }
    }
}
