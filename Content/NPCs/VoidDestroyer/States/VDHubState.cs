using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 选招 hub:只是换招的一口气(P1 14 帧 → P3 6 帧的重瞄悬停),喘息拍走完按 <see cref="VDRotation"/> 出招。
    /// 轮换表各端一致、只有权威端的返回被采纳;需要段落分隔的招在这里先做一次四角闪现,落地后新招才起拍。
    /// 阶段签名首招(变形 → 轨道轰炸,护盾 → 湮灭主炮)由转阶段状态写 ForcedNextState,这里照单执行
    /// </summary>
    [VaultState((int)VDStateIndex.Hub, typeof(VDStateContext))]
    public class VDHubState : VDStateBase
    {
        public override string StateName => "Hub";
        public override VDStateIndex StateIndex => VDStateIndex.Hub;
        /// <summary>hub 卡死只可能是目标失效,那由全局转移接走;这里给个宽松上限回到自己</summary>
        public override int TimeoutFrames => 60 * 10;

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            if (!ctx.TargetValid) {
                return null;
            }

            //重瞄:斜上方一个松弛悬停点,身体不停,眼睛在玩家身上
            Vector2 dest = ctx.Target.Center + new Vector2(ctx.SideDir * 260f, -220f);
            DeclareHoverTo(ctx, dest, 14f, 0.08f, 140f);

            if (!IsServer || Timer <= VDDirector.HubConnectorFrames(ctx.Phase) || ctx.AttackCooldown > 0) {
                return null;
            }

            VDStateIndex pick = VDRotation.Pick(ctx);
            IVDState next = VDRotation.Create(pick);
            if (next == null) {
                //注册表缺项(框架已打日志):留在 hub,下一帧再试其它招
                return null;
            }

            //每手换一次侧向:同一招左右出场不同
            ctx.SideDir = Main.rand.NextBool() ? -1 : 1;
            ctx.RandCount = 0;

            if (next is VDStateBase picked && picked.NeedsRepositionBlink) {
                ctx.CornerIndex = Main.rand.Next(4);
                ctx.Owner.StartBlink(ctx.Target.Center + VDVfx.CornerDirs[ctx.CornerIndex] * VDDirector.SwitchTeleportOffset);
            }
            MarkNetUpdate(ctx);
            return next;
        }
    }
}
