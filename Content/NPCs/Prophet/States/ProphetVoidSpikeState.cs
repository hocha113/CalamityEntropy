using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 10 号 虚空触手(原注释「虚空触手」),320 帧。
    /// <para>
    /// 节拍:倒计时 310 闪到玩家周围 400;之后倒计时高于 140 期间,
    /// 一阶段每 46 帧、二阶段每 36 帧从一个随机基准角起放出一整圈尖刺(一阶段 10 根,二阶段 12 根),
    /// 每根再带 ±0.2 的抖动。最后 140 帧只剩贴近,是收招的喘息。
    /// </para>
    /// <para>本体速度每帧直接按与玩家的差值 ×0.008 赋值,是一个恒定慢速的贴近,不会甩尾</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.VoidSpike, typeof(ProphetStateContext))]
    public class ProphetVoidSpikeState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.VoidSpike;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            int phase = ctx.Phase;

            if (ctx.Countdown == ProphetDirector.SpikeBlinkBeat && IsServer)
            {
                Teleport(ctx, target.Center + CEUtils.randomRot().ToRotationVector2() * ProphetDirector.SpikeBlinkRadius);
            }

            int period = phase == 1 ? ProphetDirector.SpikePeriodP1 : ProphetDirector.SpikePeriodP2;
            if (ctx.Countdown > ProphetDirector.SpikeActiveAbove && ctx.Countdown % period == 0 && IsServer)
            {
                //基准角与每根的抖动都是掷骰,全在权威端守卫之内
                float r = CEUtils.randomRot();
                int damage = ProjDamage(ctx);
                float step = phase == 1 ? ProphetDirector.SpikeAngleStepP1 : ProphetDirector.SpikeAngleStepP2;
                for (float i = 0; i < 360; i += step)
                {
                    Shoot<ProphetVoidSpike>(ctx, npc.Center,
                        (r + MathHelper.ToRadians(i)).ToRotationVector2().RotatedByRandom(ProphetDirector.SpikeScatter) * ProphetDirector.SpikeSpeed,
                        damage, 4, 0, npc.whoAmI);
                }
            }

            npc.velocity = (target.Center - npc.Center) * ProphetDirector.SpikeDriftLerp;
            npc.rotation = npc.velocity.ToRotation();
        }
    }
}
