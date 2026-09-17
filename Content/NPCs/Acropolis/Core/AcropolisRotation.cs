using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis.Core
{
    /// <summary>
    /// 选招:原 <c>AttackPlayer</c> 里那段 <c>TeslaCD &lt;= 0</c> 的骰点,逐条搬过来。
    /// <para>
    /// <b>保留原有的随机权重,不改成确定性序列。</b>原代码本来就有选招逻辑
    /// (两次 <c>Main.rand.NextBool(6)</c>),换成手写轮换表会直接改掉各招的出现频率,
    /// 那是手感改动,不在本次授权范围内。
    /// </para>
    /// <para>
    /// 三条必须原样保留的细节:
    /// <list type="number">
    /// <item>第二个骰子<b>无论如何都摇</b>——它写在 <c>&amp;&amp;</c> 最左边,门槛不成立也已经消耗了随机数</item>
    /// <item><c>!Jumping</c> 这道门槛留着:被鱼叉拽拉时本体是腾空的,那一刻不许起跳</item>
    /// <item>两个特招都把冷却写成 360,单发写 160;<b>冷却在出招期间照常流逝</b>,由宿主背景扣</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class AcropolisRotation
    {
        /// <summary>
        /// 骰点选下一手。<b>只该由权威端调用</b>:它带副作用(推进开火计数、重置冷却),
        /// 而 <see cref="VaultStateMachine{TContext}"/> 在客户端会照常跑 <c>OnUpdate</c> 却丢弃返回值。
        /// 返回 <see langword="null"/> 表示「这一手是单发,留在行走态」
        /// </summary>
        public static IVaultState<AcropolisStateContext> Pick(AcropolisStateContext ctx) {
            if (Main.rand.NextBool(AcropolisDirector.BarrageRollDenominator)) {
                return VaultStateRegistry<AcropolisStateContext>.Create((int)AcropolisStateIndex.CannonBarrage);
            }

            //第二个骰子必须先摇再看门槛,与原代码的短路顺序一致
            if (Main.rand.NextBool(AcropolisDirector.JumpRollDenominator) && !ctx.Airborne && ctx.HarpoonOnLauncher) {
                return VaultStateRegistry<AcropolisStateContext>.Create((int)AcropolisStateIndex.JumpShoot);
            }

            //单发:不换态,只推进开火计数。真正的开火与出膛由宿主的 ConsumeShotCue 做,
            //因为原代码的开火点在常态瞄准之后,宿主那一步才刚把枪口转过去
            ctx.TeslaCD = AcropolisDirector.TeslaCDAfterShot;
            ctx.ShotCue++;
            //决策点同步:骰子只在权威端摇,结果必须立刻过线
            ctx.Npc.netUpdate = true;
            return null;
        }
    }
}
