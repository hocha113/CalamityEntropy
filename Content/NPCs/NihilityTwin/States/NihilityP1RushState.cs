using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 一阶段 0:突进环射。
    /// <para>
    /// 两段循环:追击窗(<c>ai[0] &gt; 0</c>)里高推力扑向玩家;窗口耗尽转入射击窗,
    /// 一边缓推一边把速度乘到 30 封顶,同时细胞吐环、本体吐侧刺。
    /// 一旦被甩开 1400,两个窗都会把追击窗重新写满 36 帧(射击窗那一支还会播冲刺音)。
    /// </para>
    /// <para>
    /// 收招门槛是「计时过 360 <b>且</b>此刻正处在追击窗里」,所以贴脸缠斗时这一手会一直打下去,
    /// 直到本体自己冲过头把距离拉到 1400 以外。原代码如此,不补别的出口。
    /// </para>
    /// <para>
    /// <c>ai[0]--</c> 写在 <c>if</c> 条件里,所以<b>每帧都会减一次</b>,包括射击窗那一侧
    /// (于是射击期间它会一路减成大负数)。照搬
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P1Rush, typeof(NihilityStateContext))]
    public class NihilityP1RushState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P1Rush;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            //原写法是 if (NPC.ai[0]-- > 0):判定看的是减之前的值,而减法每帧都发生
            float chase = ctx.ChaseTimer;
            ctx.ChaseTimer = chase - 1f;
            if (chase > 0f)
            {
                if (CEUtils.getDistance(targetPos, npc.Center) > NihilityDirector.RushLeashDistance)
                {
                    ctx.ChaseTimer = NihilityDirector.RushChaseFrames;
                }
                npc.velocity += (targetPos - npc.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.RushChaseThrust;
                npc.velocity *= NihilityDirector.RushChaseDrag;
            }
            else
            {
                if (IsServer)
                {
                    if (ctx.FrameCounter % NihilityDirector.RushRingInterval == 0)
                    {
                        float rot = cell.rotation;
                        for (int i = 0; i < 360; i += NihilityDirector.RushRingStepDeg)
                        {
                            Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                                (rot + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.RushRingSpeed,
                                BulletDamage(ctx), NihilityDirector.BulletKnockback);
                        }
                    }
                    if (ctx.FrameCounter % NihilityDirector.RushSpikeInterval == 0)
                    {
                        Shoot<CellSpike>(npc.GetSource_FromThis(), npc.Center,
                            (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.RushSpikeSpeed,
                            BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                        Shoot<CellSpike>(npc.GetSource_FromThis(), npc.Center,
                            (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * NihilityDirector.RushSpikeSpeed,
                            BulletDamage(ctx), NihilityDirector.SpikeKnockback);
                    }
                }
                npc.velocity += (targetPos - npc.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.RushFireThrust;
                if (npc.velocity.Length() < NihilityDirector.RushSpeedCap)
                {
                    npc.velocity *= NihilityDirector.RushAccel;
                }
                if (CEUtils.getDistance(targetPos, npc.Center) > NihilityDirector.RushLeashDistance)
                {
                    ctx.ChaseTimer = NihilityDirector.RushChaseFrames;
                    //音效选号吃随机数,各端各选各的;不参与任何判定,照搬原位置(不进权威端门)
                    CEUtils.PlaySound("beast_ghostdash" + Main.rand.Next(1, 5), 1);
                }
            }

            npc.rotation = npc.velocity.ToRotation();
            TrailBurst(ctx);
            if (cell != null)
            {
                cell.velocity += (npc.Center - cell.Center) * NihilityDirector.RushCellPull;
            }

            ctx.Num1++;
            if (ctx.Num1 > NihilityDirector.RushDuration && ctx.ChaseTimer > 0f)
            {
                ctx.ChaseTimer = 0f;
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
