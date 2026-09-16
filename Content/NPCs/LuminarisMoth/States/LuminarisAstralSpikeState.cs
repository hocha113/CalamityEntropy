using CalamityEntropy.Content.NPCs.LuminarisMoth.Core;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth.States
{
    /// <summary>
    /// 星刺滑行:开招那一帧骰一个随机方向,滑向 800 外,滑行途中每 2 帧朝玩家撒一根刺
    /// (4 的倍数出蓝刺,否则红刺)。倒计时跌到 40 之后原地不动直到收招,
    /// 所以 <c>Utils.Remap</c> 的终点 41 帧只走到 59/60,永远差一点到不了落点——原代码如此
    /// </summary>
    [VaultState((int)LuminarisStateIndex.AstralSpike, typeof(LuminarisStateContext))]
    public class LuminarisAstralSpikeState : LuminarisStateBase
    {
        public override LuminarisStateIndex StateIndex => LuminarisStateIndex.AstralSpike;

        public override IVaultState<LuminarisStateContext> OnUpdate(LuminarisStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;
            int c = ctx.Countdown;

            npc.velocity *= 0;
            npc.rotation = 0;

            if (c == LuminarisDirector.AstralSpikeFrames)
            {
                ctx.Vec1 = npc.Center;
                if (IsServer)
                {
                    //滑行方向只在权威端骰;结果整段都在驱动位置,靠 Vec2 过线
                    ctx.Vec2 = npc.Center + CEUtils.randomRot().ToRotationVector2() * LuminarisDirector.AstralSpikeTravelDistance;
                    MarkNetUpdate(ctx);
                }
            }
            if (c > LuminarisDirector.AstralSpikeMoveEndFrame)
            {
                float p = Utils.Remap(c, LuminarisDirector.AstralSpikeFrames, LuminarisDirector.AstralSpikeMoveEndFrame, 0, 1);
                npc.Center = Vector2.Lerp(ctx.Vec1, ctx.Vec2, CEUtils.GetRepeatedCosFromZeroToOne(p, 1));
                if (c % LuminarisDirector.AstralSpikeShootInterval == 0)
                {
                    if (c % LuminarisDirector.AstralSpikeBlueInterval == 0)
                    {
                        Shoot<LuminarisSpikeBlue>(ctx, npc.Center, (player.Center - npc.Center).normalize() * LuminarisDirector.AstralSpikeProjSpeed);
                    }
                    else
                    {
                        Shoot<LuminarisSpikeRed>(ctx, npc.Center, (player.Center - npc.Center).normalize() * LuminarisDirector.AstralSpikeProjSpeed);
                    }
                }
            }

            return Tick(ctx, c);
        }
    }
}
