using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 9 号 符文飞匕(原注释「符文飞匕」),160 帧。
    /// <para>
    /// 节拍:第一帧(倒计时 160)闪到玩家周围 500~600 的随机一点,
    /// 之后倒计时高于 60 期间每 5 帧撒一把匕首,匕首初速取半径 10 的圆内随机一点(所以是慢慢散开的一团)。
    /// 最后 60 帧只剩朝向跟随,是收招的喘息。
    /// </para>
    /// <para><b>本招不写任何速度</b>:除起手瞬移清零外全程保持进招时的惯性,原代码如此</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.RuneDagger, typeof(ProphetStateContext))]
    public class ProphetRuneDaggerState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.RuneDagger;

        protected override void RunAttack(ProphetStateContext ctx) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;

            npc.rotation = (target.Center - npc.Center).ToRotation();

            if (ctx.Countdown == ProphetDirector.DaggerBlinkBeat && IsServer) {
                Teleport(ctx, target.Center + CEUtils.randomRot().ToRotationVector2()
                    * Main.rand.NextFloat(ProphetDirector.DaggerBlinkRadiusMin, ProphetDirector.DaggerBlinkRadiusMax));
            }

            if (ctx.Countdown > ProphetDirector.DaggerActiveAbove
                && ctx.Countdown % ProphetDirector.DaggerPeriod == 0 && IsServer) {
                Shoot<RuneSword>(ctx, npc.Center, CEUtils.randomPointInCircle(ProphetDirector.DaggerScatter),
                    ProjDamage(ctx), 4);
            }
        }
    }
}
