using CalamityEntropy.Content.NPCs.Cruiser.Core;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser.States
{
    /// <summary>
    /// 绕飞:锚点是「玩家看向本体的方向再旋转 0.6 弧度、外推 600」,所以本体会稳定地侧向绕圈。
    /// 每 40 帧甩一次尾鞭,共八次,350 帧收招。
    /// 尾部新星在这一手里会被削弱(环数 -2、每环减半、初速 ×0.45),削弱逻辑在链条落地那一侧
    /// </summary>
    [VaultState((int)CruiserStateIndex.AroundPlayerAndShootVoidStar, typeof(CruiserStateContext))]
    public class CruiserAroundPlayerAndShootVoidStarState : CruiserStateBase
    {
        public override CruiserStateIndex StateIndex => CruiserStateIndex.AroundPlayerAndShootVoidStar;

        /// <summary>
        /// 已发出的尾鞭次数(本地,不过线)。
        /// <para>
        /// 原判据是 <c>ChangeCounter % 40 == 0</c>,而 ChangeCounter 现在带 ±2 容差收养,
        /// 收养一步跨过 40 的倍数就少一次尾鞭、跳回去就多一次。尾鞭会写
        /// <c>whipActive / whipSpeed / flagellumAngle</c> 这三个各端都跑的累加量,
        /// 所以改成单调的「应发次数 = ChangeCounter / 40」,只在它涨上去时补发,回退不重发
        /// </para>
        /// </summary>
        private int whipsFired;

        public override void OnEnter(CruiserStateContext ctx) {
            base.OnEnter(ctx);
            whipsFired = 0;
        }

        public override IVaultState<CruiserStateContext> OnUpdate(CruiserStateContext ctx) {
            NPC npc = ctx.Npc;
            Player player = ctx.Target;

            Vector2 targetPos = player.Center
                + (npc.Center - player.Center).normalize().RotatedBy(CruiserDirector.AroundOrbitAngle) * CruiserDirector.AroundOrbitRadius;
            npc.velocity += (targetPos - npc.Center).normalize() * CruiserDirector.AroundThrust;
            npc.velocity *= CruiserDirector.AroundDrag;

            ctx.ChangeCounter++;
            //权威端每帧 +1,所以「应发次数」恰在 40 的倍数那一帧涨 1,与原等值判定逐帧等价
            int due = ctx.ChangeCounter / CruiserDirector.AroundWhipInterval;
            if (due > whipsFired) {
                int beat = due * CruiserDirector.AroundWhipInterval;
                whipsFired = due;
                //中途加入且已越过这一拍很久就静默记账,不补演出
                if (!CuePassed(ctx.ChangeCounter, beat)) {
                    ctx.TailWhipCue = true;
                    MarkNetUpdate(ctx);
                }
            }
            if (ctx.ChangeCounter > CruiserDirector.AroundDuration) {
                return NextAttack(ctx);
            }
            return null;
        }
    }
}
