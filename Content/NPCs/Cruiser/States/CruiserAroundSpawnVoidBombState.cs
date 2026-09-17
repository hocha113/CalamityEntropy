using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 巡游布雷:速度按 0.08 逼近 32、转向速率只有 0.022(比尖刺还慢),所以是一条很宽的长弧。
    /// 前 180 帧每 7 帧撒一颗虚空炸弹(散布 8,附加一份朝玩家方向 20 的初速),
    /// 之后继续绕到 340 帧收招,留出让玩家在雷区里走位的时间
    /// </summary>
    [VaultState((int)CruiserStateIndex.AroundSpawnVoidBomb, typeof(CruiserStateContext))]
    public class CruiserAroundSpawnVoidBombState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.AroundSpawnVoidBomb;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            npc.velocity = npc.velocity.normalize()
                * (npc.velocity.Length() + (CruiserDirector.BombSpeedTarget - npc.velocity.Length()) * CruiserDirector.BombSpeedLerp);
            npc.velocity = CEUtils.RotateTowardsAngle(npc.velocity.ToRotation(),
                (player.Center - npc.Center).ToRotation(), CruiserDirector.BombTurnRate, false).ToRotationVector2()
                * npc.velocity.Length();

            ctx.ChangeCounter++;
            //取模等值判定安全:IsServer 就写在同一个条件里,拍体只有权威端动作,
            //而 ChangeCounter 的容差收养只发生在客户端;权威端每帧稳定 +1,每 7 帧命中恰好一次
            if (ctx.ChangeCounter < CruiserDirector.BombWindow
                && ctx.ChangeCounter % CruiserDirector.BombInterval == 0
                && IsServer) {
                Shoot(ctx, ModContent.ProjectileType<VoidBomb>(), npc.Center,
                    CEUtils.randomPointInCircle(CruiserDirector.BombScatter)
                        + (player.Center - npc.Center).normalize() * CruiserDirector.BombLead);
                MarkNetUpdate(ctx);
            }
            if (ctx.ChangeCounter > CruiserDirector.BombDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
