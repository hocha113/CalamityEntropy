using CalamityEntropy.Content.NPCs.Acropolis.Core;
using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.Acropolis.States
{
    /// <summary>
    /// 行走(选招口)。地面推进、悬停、腿部步态、鱼叉循环都由宿主当背景行为每帧跑,
    /// 本状态只负责三件事:跨招冷却到点时骰点、单发电球留在本态、满足条件时请求追高跳。
    /// <para>
    /// 判定顺序照搬原代码:骰点写在地面推进块<b>之前</b>,所以同一帧里骰点优先。
    /// 骰到单发时不换态,于是「单发 + 追高跳同帧发生」仍然成立,与原代码一致
    /// </para>
    /// </summary>
    [VaultState((int)AcropolisStateIndex.Walk, typeof(AcropolisStateContext))]
    public class AcropolisWalkState : AcropolisStateBase
    {
        public override AcropolisStateIndex StateIndex => AcropolisStateIndex.Walk;

        /// <summary>选招口本身不该超时,超时兜底只对实招有意义</summary>
        public override int TimeoutFrames => int.MaxValue;

        public override IVaultState<AcropolisStateContext> OnUpdate(AcropolisStateContext ctx)
        {
            //跨招冷却由宿主每帧按 enrange 扣,这里只判到点。骰子只在权威端摇
            if (ctx.TeslaCD <= 0f && IsServer)
            {
                IVaultState<AcropolisStateContext> next = AcropolisRotation.Pick(ctx);
                if (next != null)
                {
                    return next;
                }
            }

            //追高跳:原代码写在地面推进块里的 JumpCD <= -260 那一支
            if (ctx.Grounded && ctx.HarpoonOnLauncher && !ctx.Airborne
                && ctx.Player.Center.Y + AcropolisDirector.LeapHeightGate * ctx.Npc.scale < ctx.Npc.Center.Y
                && ctx.Owner.JumpCD <= AcropolisDirector.LeapReadyJumpCD)
            {
                return IsServer ? Create(AcropolisStateIndex.Leap) : null;
            }

            return null;
        }
    }
}
