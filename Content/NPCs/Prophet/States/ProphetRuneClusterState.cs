using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 2 号 符文晶簇(原注释「符文晶簇」),360 帧(原式 <c>120 + 60 * 4</c>)。
    /// <para>
    /// 节拍:每 60 帧一循环,整 60 拍闪到玩家侧方并朝玩家冲 8,半拍(余 30)吐出高速晶体。
    /// 循环只在倒计时高于 110 时进行;低于 120 后本体转为向玩家侧后方 400 的位置回收。
    /// 注意 110 和 120 两条线<b>不重合</b>,倒计时 119~110 这十帧既回收又还在循环窗里,原代码如此。
    /// </para>
    /// <para>朝向恒等于速度方向,所以晶体射向就是当前航向,闪现后的那一下冲刺即为预告</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.RuneCluster, typeof(ProphetStateContext))]
    public class ProphetRuneClusterState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.RuneCluster;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float difficult = ctx.Difficult;
            int phase = ctx.Phase;
            int cd = ctx.Countdown;

            npc.velocity *= ProphetDirector.ClusterDrag;
            if (cd < ProphetDirector.ClusterHomeBelow)
            {
                npc.velocity += (npc.Center - (target.Center + (npc.Center - target.Center).normalize()
                    .RotatedBy(ProphetDirector.ClusterHomeAngle) * ProphetDirector.ClusterHomeRadius)).normalize()
                    * ProphetDirector.ClusterHomeAccel;
            }
            npc.rotation = npc.velocity.ToRotation();

            if (cd >= ProphetDirector.ClusterActiveAbove && cd % ProphetDirector.ClusterPeriod == ProphetDirector.ClusterBlinkPhase)
            {
                if (IsServer)
                {
                    Teleport(ctx, target.Center + target.velocity.SafeNormalize(CEUtils.randomRot().ToRotationVector2())
                        * ProphetDirector.ClusterBlinkRadius / difficult);
                }
                //起跑速度是运动,各端都写;客户端此刻还没收到落点,下一包会把位置与速度一起纠回来
                npc.velocity = (target.Center - npc.Center).normalize() * ProphetDirector.ClusterLaunchSpeed;
            }

            if (cd >= ProphetDirector.ClusterActiveAbove && cd % ProphetDirector.ClusterPeriod == ProphetDirector.ClusterFirePhase)
            {
                //晶簇单独走 damage/6 - 5
                int damage = ProjDamage(ctx) + ProphetDirector.CrystalDamageOffset;
                Vector2 aim = npc.rotation.ToRotationVector2();
                int shots = phase == 1 ? ProphetDirector.ClusterShotsP1 : ProphetDirector.ClusterShotsP2;
                for (int i = 0; i < shots; i++)
                {
                    if (i == 0)
                    {
                        Shoot<RuneCrystalTop>(ctx, npc.Center, aim * ProphetDirector.ClusterSpeed, damage, 4);
                    }
                    else
                    {
                        Shoot<RuneCrystalTop>(ctx, npc.Center,
                            aim.RotatedBy(ProphetDirector.ClusterSpread * i) * ProphetDirector.ClusterSpeed, damage, 4);
                        Shoot<RuneCrystalTop>(ctx, npc.Center,
                            aim.RotatedBy(-ProphetDirector.ClusterSpread * i) * ProphetDirector.ClusterSpeed, damage, 4);
                    }
                }
            }
        }
    }
}
