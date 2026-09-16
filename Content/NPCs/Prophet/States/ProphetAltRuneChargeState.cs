using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 11 号 异形符文冲锋(原代码这一支没写注释),242 帧。
    /// <para>
    /// 节拍:倒计时 239 闪到远处;228 在自身周围撒一圈静止的异形符文;
    /// 210 / 140 / 70 各起手一次直线冲锋,锁向玩家 8 帧后的预测点并把冲锋窗写进 <c>ai[1]</c>(62 帧),
    /// 窗口内轻阻尼 0.99,窗口外硬刹 0.8。
    /// </para>
    /// <para>
    /// 布阵的角度上限写的是 <b>358</b> 而不是 360,所以一阶段步进 72° 只排得下 5 个锚点
    /// (0/72/144/216/288,下一个 360 越界),二阶段步进 60° 排得下 6 个。原代码如此,照搬。
    /// </para>
    /// <para>接触伤害在本招与 1 号招打开(<c>CanHitPlayer</c> 读 <c>ai[3]</c>)</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.AltRuneCharge, typeof(ProphetStateContext))]
    public class ProphetAltRuneChargeState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.AltRuneCharge;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            float difficult = ctx.Difficult;
            int phase = ctx.Phase;
            int cd = ctx.Countdown;

            if (cd == ProphetDirector.AltBlinkBeat && IsServer)
            {
                Teleport(ctx, target.Center + CEUtils.randomRot().ToRotationVector2()
                    * ProphetDirector.AltBlinkRadius / difficult);
            }

            if (cd == ProphetDirector.AltRuneBeat && IsServer)
            {
                //原代码这一块漏了 netMode 守卫,客户端会各自多生成一圈只存在于本地的符文;
                //按联机契约收归权威端,弹幕本身由服务端广播
                float r = CEUtils.randomRot();
                int damage = ProjDamage(ctx);
                float step = phase == 1 ? ProphetDirector.AltRuneStepP1 : ProphetDirector.AltRuneStepP2;
                for (float i = 0; i < ProphetDirector.AltRuneAngleLimit; i += step)
                {
                    Shoot<ProphetRuneAlt>(ctx, npc.Center, Vector2.Zero, damage, 2, npc.whoAmI,
                        r + MathHelper.ToRadians(i),
                        Main.rand.Next(ProphetDirector.OrbRuneVariantMin, ProphetDirector.OrbRuneVariantMax));
                }
            }

            if (cd == ProphetDirector.AltChargeBeatA || cd == ProphetDirector.AltChargeBeatB || cd == ProphetDirector.AltChargeBeatC)
            {
                //锁向 + 一帧定速:运动,各端都跑。朝向是持久累加量,决策点当场过线
                npc.rotation = (PredictTarget(ctx, ProphetDirector.AltChargeLeadFrames) - npc.Center).ToRotation();
                npc.ai[1] = ProphetDirector.AltChargeFrames;
                npc.velocity = npc.rotation.ToRotationVector2() * difficult
                    * (phase == 1 ? ProphetDirector.AltChargeSpeedP1 : ProphetDirector.AltChargeSpeedP2)
                    * ProphetDirector.AltChargeSpeedBase;
                MarkNetUpdate(ctx);
            }

            if (npc.ai[1] > 0)
            {
                npc.ai[1]--;
                npc.velocity *= ProphetDirector.AltChargeDrag;
            }
            else
            {
                npc.velocity *= ProphetDirector.AltBrakeDrag;
            }
        }
    }
}
