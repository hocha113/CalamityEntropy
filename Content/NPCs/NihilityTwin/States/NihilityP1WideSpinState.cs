using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 一阶段 4:广角自旋。本体运动与 1 号同构,但自旋量只取 0.26 折、细胞被甩到 520 外,
    /// 于是细胞绕着本体划一个大圆。每 30 帧一次:先朝玩家打一发十一枚的箭形扇面,再叠一圈随机相位的六发环。
    /// <para>
    /// 扇面按层生成:第 0 层单发走中路、伤害除数是 <b>7</b> 而不是 6(原代码唯一一处不同的除数),
    /// 第 1~5 层各出上下两发,横向后退 30×层、纵向张开 14×层,全部同速 14。
    /// </para>
    /// <para>细胞挂点这里是<b>整段覆盖</b>速度而不是 1 号的累加,所以细胞被硬拽在圆周上</para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P1WideSpin, typeof(NihilityStateContext))]
    public class NihilityP1WideSpinState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P1WideSpin;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            ctx.KeepRotSpeed = true;

            cell.rotation = npc.rotation;
            npc.velocity *= NihilityDirector.WideDrag;
            npc.velocity = (targetPos - npc.Center) * NihilityDirector.WideFollow;
            npc.rotation += ctx.RotSpeed * NihilityDirector.WideRotScale;
            ctx.RotSpeed += NihilityDirector.WideRotAccel;
            ctx.RotSpeed *= NihilityDirector.WideRotDamp;
            cell.velocity *= NihilityDirector.WideCellDrag;
            cell.velocity = (npc.Center + npc.rotation.ToRotationVector2() * NihilityDirector.WideCellOffset - cell.Center) * NihilityDirector.WideCellLerp;

            if (ctx.Num1 > NihilityDirector.WideWindup && ctx.FrameCounter % NihilityDirector.WideFireInterval == 0 && IsServer)
            {
                float rot = (targetPos - cell.Center).ToRotation();
                for (int i = 0; i < NihilityDirector.WideFanLayers; i++)
                {
                    if (i > 0)
                    {
                        Shoot<CellBullet>(cell.GetSource_FromThis(),
                            cell.Center + new Vector2(i * NihilityDirector.WideFanBackStep, i * NihilityDirector.WideFanSideStep).RotatedBy(rot),
                            rot.ToRotationVector2() * NihilityDirector.WideFanSpeed,
                            BulletDamage(ctx), NihilityDirector.BulletKnockback);
                        Shoot<CellBullet>(cell.GetSource_FromThis(),
                            cell.Center + new Vector2(i * NihilityDirector.WideFanBackStep, i * -NihilityDirector.WideFanSideStep).RotatedBy(rot),
                            rot.ToRotationVector2() * NihilityDirector.WideFanSpeed,
                            BulletDamage(ctx), NihilityDirector.BulletKnockback);
                    }
                    else
                    {
                        Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                            rot.ToRotationVector2() * NihilityDirector.WideFanSpeed,
                            npc.damage / NihilityDirector.ProjDamageDivisorCenter, NihilityDirector.BulletKnockback);
                    }
                }
                rot = CEUtils.randomRot();
                for (int i = 0; i < 360; i += NihilityDirector.WideRingStepDeg)
                {
                    Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                        (rot + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.WideRingSpeed,
                        BulletDamage(ctx), NihilityDirector.BulletKnockback);
                }
            }

            ctx.Num1++;
            if (ctx.Num1 > NihilityDirector.WideDuration)
            {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
