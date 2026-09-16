using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 5 号 环状符文(原注释「环状符文」),245 帧。
    /// <para>
    /// 节拍:倒计时高于 160 是连续闪现段,一阶段每 4 帧、二阶段每 3 帧闪到玩家周围 1400 的随机一点,
    /// 每闪一次朝玩家吐一发慢速洪流并把朝向对准玩家 —— 玩家看到的是一圈从四面八方逼近的弹幕。
    /// 160 以下转普通追击(阻尼 0.96 + 朝玩家推 0.5),给一段喘息。
    /// </para>
    /// <para>一阶段约 21 次、二阶段约 28 次闪现,每一次都是权威端掷骰并当场过线的决策点</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.RingBlink, typeof(ProphetStateContext))]
    public class ProphetRingBlinkState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.RingBlink;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float difficult = ctx.Difficult;
            int phase = ctx.Phase;

            if (ctx.Countdown > ProphetDirector.RingBlinkUntil)
            {
                int period = phase == 1 ? ProphetDirector.RingBlinkPeriodP1 : ProphetDirector.RingBlinkPeriodP2;
                if (ctx.Countdown % period == 0)
                {
                    if (IsServer)
                    {
                        Teleport(ctx, target.Center + CEUtils.randomRot().ToRotationVector2()
                            * ProphetDirector.RingBlinkRadius / difficult);
                    }
                    CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), npc.Center);
                    Shoot<RuneTorrent>(ctx, npc.Center,
                        (target.Center - npc.Center).normalize() * difficult * ProphetDirector.RingTorrentSpeed,
                        ProjDamage(ctx), 4, ProphetDirector.RingTorrentMaxSpeed * difficult);
                    npc.rotation = (target.Center - npc.Center).ToRotation();
                }
            }
            else
            {
                npc.rotation = npc.velocity.ToRotation();
                npc.velocity *= ProphetDirector.RingChaseDrag;
                npc.velocity += (target.Center - npc.Center).normalize() * ProphetDirector.RingChaseThrust;
            }
        }
    }
}
