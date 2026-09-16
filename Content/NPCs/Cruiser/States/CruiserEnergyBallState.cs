using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 能量球:开局那一帧放出一颗挂在本体上的能量球(ai0 = 本体 whoAmI),自己慢速贴近 240 帧。
    /// 注意原代码把收招判定写在速度写入<b>之前</b>,所以收招那一帧的推进照样执行,这里保留同一顺序
    /// </summary>
    [VaultState((int)CruiserStateIndex.EnergyBall, typeof(CruiserStateContext))]
    public class CruiserEnergyBallState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.EnergyBall;

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            if (ctx.ChangeCounter == 0) {
                Shoot(ctx, ModContent.ProjectileType<CruiserEnergyBall>(), npc.Center, Vector2.Zero,
                    CruiserDirector.EnergyBallDamageMult, npc.whoAmI);
                MarkNetUpdate(ctx);
            }
            ctx.ChangeCounter++;

            IVaultState<CruiserStateContext> next = null;
            if (ctx.ChangeCounter > CruiserDirector.EnergyBallDuration) {
                next = NextAttack(ctx);
            }
            npc.velocity += (player.Center - npc.Center).normalize()
                * (npc.Distance(player.Center) > CruiserDirector.EnergyBallFarDistance
                    ? CruiserDirector.EnergyBallThrustFar : CruiserDirector.EnergyBallThrustNear);
            npc.velocity *= CruiserDirector.EnergyBallDrag;
            return next;
        }
    }
}
