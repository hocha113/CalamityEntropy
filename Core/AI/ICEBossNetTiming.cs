namespace CalamityEntropy.Core.AI
{
    /// <summary>
    /// 能随快照过线的状态:各 Boss 的状态基类实现它,宿主便无需知道具体状态类型。
    /// StateId/Timer/Counter 由 VaultState 自带,只需补收养方法
    /// </summary>
    public interface ICEBossNetTiming
    {
        int StateId { get; }
        int Timer { get; }
        int Counter { get; }
        /// <summary>客户端:收养权威端的计时(带容差,见 <see cref="CEBossNetMotion.AdoptTimer"/>)</summary>
        void AdoptNetTiming(int timer, int counter);
    }
}
