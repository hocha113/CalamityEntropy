using CalamityEntropy.Content.NPCs.Acropolis.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis.States
{
    /// <summary>
    /// 追高跳:原代码里 <c>JumpCD &lt;= -260</c> 的那一支。玩家高出本体 200×scale 以上、
    /// 鱼叉在架上、跳跃冷却已经透支 260 帧时,按水平差 0.01、垂直差 0.08(最高 -30)弹射上去。
    /// <para>
    /// 它不开火,只是位移;炮口这段时间走宿主的常态瞄准。
    /// 收招同样交给宿主的落地判定——本状态的跳射计数是负数,所以落地闸天然打开
    /// </para>
    /// </summary>
    [VaultState((int)AcropolisStateIndex.Leap, typeof(AcropolisStateContext))]
    public class AcropolisLeapState : AcropolisStateBase
    {
        public override AcropolisStateIndex StateIndex => AcropolisStateIndex.Leap;

        public override void OnEnter(AcropolisStateContext ctx)
        {
            base.OnEnter(ctx);
            NPC npc = ctx.Npc;
            ctx.Airborne = true;
            ctx.Owner.JumpCD = AcropolisDirector.LeapJumpCD;
            if (ctx.Target != null)
            {
                npc.velocity = new Vector2(
                    AcropolisDirector.LeapSpeedXFactor * (ctx.Target.Center.X - npc.Center.X) / npc.scale,
                    float.Max((ctx.Target.Center.Y - npc.Center.Y) / npc.scale * AcropolisDirector.LeapSpeedYFactor, AcropolisDirector.LeapSpeedYMax))
                    * npc.scale;
            }
        }

        public override IVaultState<AcropolisStateContext> OnUpdate(AcropolisStateContext ctx)
        {
            if (!ctx.Airborne)
            {
                return BackToWalk(ctx);
            }
            return null;
        }
    }
}
