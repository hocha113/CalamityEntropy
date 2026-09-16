using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using CalamityEntropy.Content.Projectiles;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.NihilityTwin.States
{
    /// <summary>
    /// 二阶段 4:分裂增殖。第 2 帧从细胞身上放出三只小细胞(它们自带缠绳并自行骚扰玩家),
    /// 随后细胞持续扑玩家、每 30 帧一圈九发,本体只做减速跟随。
    /// <para>
    /// 场上小细胞超过 8 只时这一手会在选招阶段被重掷掉(见 <see cref="NihilityRotation.Pick"/>),
    /// 那是原代码里唯一的出招抑制。
    /// </para>
    /// <para>
    /// <b>与原代码的一处差异</b>:三只小细胞的 <c>NPC.NewNPC</c> 在原代码里没有权威端守卫,
    /// 客户端会各自造三只不同步的幽灵。这里补上守卫,单机行为不变
    /// </para>
    /// </summary>
    [VaultState((int)NihilityStateIndex.P2Split, typeof(NihilityStateContext))]
    public class NihilityP2SplitState : NihilityStateBase
    {
        public override NihilityStateIndex StateIndex => NihilityStateIndex.P2Split;

        public override IVaultState<NihilityStateContext> OnUpdate(NihilityStateContext ctx) {
            NPC npc = ctx.Npc;
            NPC cell = ctx.Cell;
            Vector2 targetPos = ctx.Target.Center;

            if (ctx.Num1 == NihilityDirector.SpawnCueFrame && IsServer) {
                for (int i = 0; i < NihilityDirector.SpawnCellCount; i++) {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)cell.Center.X, (int)cell.Center.Y,
                        ModContent.NPCType<ChaoticCellSmall>(), 0, cell.whoAmI);
                }
            }
            npc.rotation = npc.velocity.ToRotation();
            ctx.Num1++;
            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * NihilityDirector.SplitCellThrust;

            if (IsServer && ctx.FrameCounter % NihilityDirector.SplitRingInterval == 0) {
                float rot = CEUtils.randomRot();
                for (int i = 0; i < 360; i += NihilityDirector.SplitRingStepDeg) {
                    Shoot<CellBullet>(cell.GetSource_FromThis(), cell.Center,
                        (rot + MathHelper.ToRadians(i)).ToRotationVector2() * NihilityDirector.SplitRingSpeed,
                        BulletDamage(ctx), NihilityDirector.BulletKnockback);
                }
            }

            npc.velocity *= NihilityDirector.SplitDrag;
            if (ctx.Num1 > NihilityDirector.SplitDuration) {
                return EndAttack(ctx);
            }
            return null;
        }
    }
}
