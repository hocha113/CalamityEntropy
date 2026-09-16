using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 一阶段 1:自旋狙击。本体悬在玩家附近自旋,细胞挂在身后 120 处、朝本体<b>背面</b>连射,
    /// 于是弹幕是一条随自旋扫过全场的射线。
    /// <para>
    /// 自旋角速度 <c>RotSpeed</c> 每帧 <c>+0.134</c> 再 <c>×0.62</c>,稳态约 0.218 rad/帧。
    /// 它是本状态与一阶段 4 号、二阶段 1 号共用的持久累加量,离开这三手时由宿主清零。
    /// </para>
    /// <para>
    /// 原代码先写了一句 <c>NPC.velocity *= 0.98f</c>,紧接着又整段覆盖成位置弹簧——
    /// 前一句实际不起作用,照搬保留。开火条件里还有一个恒真的 <c>counter % 1 == 0</c>,等价于每帧,这里略去
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P1SpinSnipe, typeof(NihilityStateContext))]
    public class NihilityP1SpinSnipeState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P1SpinSnipe;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            ctx.KeepRotSpeed = true;

            cell.rotation = npc.rotation;
            npc.velocity *= NihilityDirector.SpinDrag;
            npc.velocity = (targetPos - npc.Center) * NihilityDirector.SpinFollow;
            npc.rotation += ctx.RotSpeed;
            ctx.RotSpeed += NihilityDirector.SpinRotAccel;
            ctx.RotSpeed *= NihilityDirector.SpinRotDamp;
            cell.velocity *= NihilityDirector.SpinCellDrag;
            cell.velocity += (npc.Center + npc.rotation.ToRotationVector2() * NihilityDirector.SpinCellOffset - cell.Center) * NihilityDirector.SpinCellLerp;

            if (ctx.Num1 > NihilityDirector.SpinWindup && IsServer
                && CEUtils.getDistance(cell.Center, npc.Center) < NihilityDirector.SpinFireRange)
            {
                Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                    npc.rotation.ToRotationVector2() * NihilityDirector.SpinBulletSpeed,
                    BulletDamage(ctx), NihilityDirector.BulletKnockback);
            }

            ctx.Num1++;
            IVaultState<NihilityStateContext> next = null;
            if (ctx.Num1 > NihilityDirector.SpinDuration)
            {
                next = EndAttack(ctx);
            }
            //原代码在收招判定之后还额外推了两次绳索求解:自旋时绳子才跟得上,纯绘制
            ctx.Owner.TickRope();
            ctx.Owner.TickRope();
            return next;
        }
    }
}
