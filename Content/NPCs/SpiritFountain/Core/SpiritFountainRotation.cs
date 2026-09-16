using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.SpiritFountain.Core
{
    /// <summary>
    /// 选招裁决。这只 Boss <b>没有轮换表</b>:原 AI() 里七个状态块是自上而下的顺序 <c>if</c>,
    /// 每个块自己写死了下一手,所以出招序列是一条固定的链,一处随机都没有。
    /// <para>
    /// 链本身:<c>出场演出 → 横扫 → 回旋 → 激光 → 落环喷泉 → 横扫 →…</c>,
    /// 另有一条由血量驱动的一次性插入:血量跌破 66%(阶段号 &gt; 3)时无条件切到转阶段演出,
    /// 演出结束进入十字斩,此后<b>再也不回主链</b>——十字斩是终局循环态,靠自己原地重置计时反复重开。
    /// </para>
    /// <para>
    /// <b>不引入防复读阀、不改链序。</b>链是确定性的,加查重窗只会改变出招序列,
    /// 那是手感改动,不在本次无损迁移的范围内。
    /// </para>
    /// </summary>
    public static class SpiritFountainRotation
    {
        /// <summary>由注册表创建状态实例(未注册返回 null,框架已打日志)</summary>
        public static IVaultState<SpiritFountainStateContext> Create(SpiritFountainStateIndex state)
            => VaultStateRegistry<SpiritFountainStateContext>.Create((int)state);

        /// <summary>
        /// 线性链的下一手。<b>只该由权威端调用</b>(门开在 <see cref="SpiritFountainStateBase.Advance"/> 上)。
        /// 这里不清任何标量:原代码的换态语句只写 <c>aiTimer = 0</c>,
        /// 而 <c>aiTimer</c> 已由新状态的 <c>OnEnter</c> 归零;<c>num1</c> 是回旋段自己在换态那一行清的,
        /// <c>Counter</c> / <c>mCounter</c> / <c>mAmp</c> 则<b>故意不清</b>
        /// (Counter 永不归零;摇摆量靠挂在横扫块上的 else 每帧清)
        /// </summary>
        public static IVaultState<SpiritFountainStateContext> Pick(SpiritFountainStateContext ctx, SpiritFountainStateIndex from)
            => Create(Next(from));

        /// <summary>链序映射,逐条对应原代码换态语句里的 <c>ai = AIStyle.X</c></summary>
        public static SpiritFountainStateIndex Next(SpiritFountainStateIndex from) => from switch
        {
            SpiritFountainStateIndex.SpawnAnimation => SpiritFountainStateIndex.Moving,
            SpiritFountainStateIndex.Moving => SpiritFountainStateIndex.Boomerang,
            SpiritFountainStateIndex.Boomerang => SpiritFountainStateIndex.Lasers,
            SpiritFountainStateIndex.Lasers => SpiritFountainStateIndex.RingFountains,
            SpiritFountainStateIndex.RingFountains => SpiritFountainStateIndex.Moving,
            SpiritFountainStateIndex.PhaseTranse1 => SpiritFountainStateIndex.SpiritSlicing,
            //十字斩不走换态,它在状态体里原地把计时归零重开。这一条只是超时兜底时的去处
            SpiritFountainStateIndex.SpiritSlicing => SpiritFountainStateIndex.SpiritSlicing,
            _ => SpiritFountainStateIndex.Moving,
        };

        /// <summary>
        /// 状态块在原 AI() 里的书写顺序。
        /// <para>
        /// 这是无损迁移的关键:原代码七个块是顺序 <c>if</c> 而不是 <c>else if</c>,所以在第 k 块里换到
        /// <b>更靠后</b>的状态时,新块会在<b>同一帧</b>接着跑(读到的 aiTimer 是 0);
        /// 换回<b>更靠前</b>的状态则要等下一帧,而那一帧开头的统一自增会让它从 1 起跑。
        /// 宿主的续跑链就按这张表决定同帧续跑还是延后。
        /// </para>
        /// </summary>
        public static int ChainOrder(SpiritFountainStateIndex index) => index switch
        {
            SpiritFountainStateIndex.SpawnAnimation => 0,
            SpiritFountainStateIndex.Moving => 2,
            SpiritFountainStateIndex.Boomerang => 3,
            SpiritFountainStateIndex.Lasers => 4,
            SpiritFountainStateIndex.RingFountains => 5,
            SpiritFountainStateIndex.PhaseTranse1 => 6,
            SpiritFountainStateIndex.SpiritSlicing => 7,
            _ => 0,
        };

        /// <summary>同上,取实例的链序;不是本 Boss 的状态就按「最靠后」处理,不续跑</summary>
        public static int ChainOrder(IVaultState<SpiritFountainStateContext> state)
            => state is SpiritFountainStateBase typed ? ChainOrder(typed.StateIndex) : int.MaxValue;

        /// <summary>
        /// 血量触发的转阶段插入件在原 AI() 里的位置:出场演出块之后、横扫块之前。
        /// 所以它切出去的转阶段演出(链序 6)永远是同帧续跑
        /// </summary>
        public const int PhaseTriggerOrder = 1;
    }
}
