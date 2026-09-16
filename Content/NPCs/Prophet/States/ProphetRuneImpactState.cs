using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 7 号 符文冲击(原注释「符文冲击」),280 帧。
    /// <para>
    /// 节拍:倒计时高于 50 时以 40 帧为一循环,余数 21~39 是绕行窗(贴着玩家外侧 160 的环走),
    /// 余数 0~20 是放电窗(刹车 0.94),其中余数正好 20 的那一帧放一把扇形闪电。
    /// 倒计时 50 以下永远停在绕行窗,是收招前的一段纯贴身压迫。
    /// </para>
    /// <para>公平阀:绕行窗的转向只有 0.06,先知会明显地「摆正炮口」,放电前有大约半秒的可读时间</para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.RuneImpact, typeof(ProphetStateContext))]
    public class ProphetRuneImpactState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.RuneImpact;

        protected override void RunAttack(ProphetStateContext ctx) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            int phase = ctx.Phase;
            int cd = ctx.Countdown;

            bool orbit = true;
            //原代码写的是 if (余数 > 20) { 空块 } else { ... };这里直接取反,语义一致
            if (cd > ProphetDirector.ImpactActiveAbove) {
                if (cd % ProphetDirector.ImpactPeriod <= ProphetDirector.ImpactFireRemainder) {
                    orbit = false;
                    if (cd % ProphetDirector.ImpactPeriod == ProphetDirector.ImpactFireRemainder) {
                        int damage = ProjDamage(ctx);
                        Vector2 aim = (target.Center - npc.Center).normalize();
                        float spread = phase == 1 ? ProphetDirector.ImpactSpreadP1 : ProphetDirector.ImpactSpreadP2;
                        int layers = phase == 1 ? ProphetDirector.ImpactLayersP1 : ProphetDirector.ImpactLayersP2;
                        //判定是 <=,所以含正中那一发共 layers + 1 层
                        for (int i = 0; i <= layers; i++) {
                            if (i == 0) {
                                Shoot<ProphetLightning>(ctx, npc.Center, aim * ProphetDirector.ImpactBoltSpeed, damage, 4);
                            }
                            else {
                                Shoot<ProphetLightning>(ctx, npc.Center,
                                    aim.RotatedBy(i * spread) * ProphetDirector.ImpactBoltSpeed, damage, 4);
                                Shoot<ProphetLightning>(ctx, npc.Center,
                                    aim.RotatedBy(i * -spread) * ProphetDirector.ImpactBoltSpeed, damage, 4);
                            }
                        }
                    }
                    npc.velocity *= ProphetDirector.ImpactBrakeDrag;
                }
            }

            if (orbit) {
                npc.velocity = (target.Center + (npc.Center - target.Center).normalize() * ProphetDirector.ImpactOrbitRadius
                    - npc.Center) * ProphetDirector.ImpactOrbitLerp;
                npc.rotation = CEUtils.RotateTowardsAngle(npc.rotation, npc.velocity.ToRotation(),
                    ProphetDirector.ImpactOrbitRotate, false);
            }
        }
    }
}
