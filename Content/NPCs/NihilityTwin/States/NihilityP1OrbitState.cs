using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 一阶段 5:对拉旋转。开场锁一次轴向并给全体玩家 60 帧无敌,随后本体与细胞分居玩家两侧 840,
    /// 靠本体每帧自转 1.6° 把整根轴慢慢转起来,玩家被夹在中间。
    /// <para>
    /// 开火窗 80~460:每帧 1/3 概率出一发,再 1/2 分流成细胞弹或虚无火。两种弹的出射角
    /// 直接取全局游戏帧数 ×±0.09,也就是一条持续旋转的散射线。生成在权威端,角度自然以权威端为准。
    /// </para>
    /// <para>460 帧后两端一起上浮 1.2/帧,500 帧收招——那 40 帧是纯粹的脱离动作</para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P1Orbit, typeof(NihilityStateContext))]
    public class NihilityP1OrbitState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P1Orbit;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx)
        {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            if (ctx.Num1 == 0)
            {
                //原代码连播两次同一条起手音,照搬
                CEUtils.PlaySound("charge", 1, npc.Center);
                CEUtils.PlaySound("charge", 1, npc.Center);
                npc.rotation = (npc.Center - targetPos).ToRotation();
                //无敌帧要在各端各自写:玩家受击判定本来就跑在自己那台机器上
                foreach (Player player in Main.ActivePlayers)
                {
                    player.Entropy().immune = NihilityDirector.OrbitGraceFrames;
                }
            }
            ctx.Num1++;

            Vector2 t = npc.rotation.ToRotationVector2() * NihilityDirector.OrbitRadius;
            if (ctx.Num1 < NihilityDirector.OrbitReleaseFrame)
            {
                npc.velocity = (targetPos + t - npc.Center) * NihilityDirector.OrbitFollow;
                cell.velocity = (targetPos - t - cell.Center) * NihilityDirector.OrbitFollow;
            }
            else
            {
                npc.velocity.Y -= NihilityDirector.OrbitRiseAccel;
                cell.velocity.Y -= NihilityDirector.OrbitRiseAccel;
            }

            if (IsServer && ctx.Num1 > NihilityDirector.OrbitFireStart && ctx.Num1 < NihilityDirector.OrbitReleaseFrame
                && Main.rand.NextBool(NihilityDirector.OrbitFireChance))
            {
                if (Main.rand.NextBool(NihilityDirector.OrbitSplitChance))
                {
                    Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                        (Main.GameUpdateCount * NihilityDirector.OrbitBulletSpin).ToRotationVector2() * NihilityDirector.OrbitBulletSpeed,
                        BulletDamage(ctx), NihilityDirector.BulletKnockback);
                }
                else
                {
                    Shoot<NihilityFire>(npc.GetSource_FromThis(), npc.Center,
                        (Main.GameUpdateCount * NihilityDirector.OrbitFireSpin).ToRotationVector2() * NihilityDirector.OrbitFireSpeed,
                        BulletDamage(ctx), NihilityDirector.BulletKnockback);
                }
            }

            IVaultState<NihilityStateContext> next = null;
            if (ctx.Num1 > NihilityDirector.OrbitDuration)
            {
                next = EndAttack(ctx);
            }
            npc.rotation += MathHelper.ToRadians(NihilityDirector.OrbitSelfSpinDeg);
            return next;
        }
    }
}
