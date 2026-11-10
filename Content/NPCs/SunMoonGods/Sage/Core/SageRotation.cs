using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core
{
    /// <summary>
    /// 每阶段一张固定表按序循环,相邻两手不同类,没有随机也没有替补
    /// 转阶段时序号归零,表头就是该阶段的签名招
    /// 只权威端调
    /// </summary>
    public static class SageRotation
    {
        private static readonly SageStateIndex[] Phase1 =
        {
            SageStateIndex.StarTorrent, SageStateIndex.CometCharge, SageStateIndex.StarRing,
            SageStateIndex.CrescentSlash, SageStateIndex.CometCharge, SageStateIndex.CrossStarburst,
        };

        private static readonly SageStateIndex[] Phase2 =
        {
            SageStateIndex.MeteorShower, SageStateIndex.CometCharge, SageStateIndex.StarTorrent, SageStateIndex.StarRing,
            SageStateIndex.CrescentSlash, SageStateIndex.CometCharge, SageStateIndex.CrossStarburst,
        };

        private static readonly SageStateIndex[] Phase3 =
        {
            SageStateIndex.FullMoonBeam, SageStateIndex.CometCharge, SageStateIndex.MeteorShower, SageStateIndex.StarRing,
            SageStateIndex.CrescentSlash, SageStateIndex.StarTorrent, SageStateIndex.CometCharge, SageStateIndex.CrossStarburst,
        };

        public static SageStateIndex[] TableFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        public static SageStateBase Create(SageStateIndex state)
            => VaultStateRegistry<SageStateContext>.Create((int)state) as SageStateBase;

        /// <summary>取表里当前一格并推进序号</summary>
        public static SageStateIndex Pick(SageStateContext ctx) {
            SageStateIndex[] table = TableFor(ctx.Phase);
            int slot = ((ctx.AttackIndex % table.Length) + table.Length) % table.Length;
            ctx.AttackIndex = (slot + 1) % table.Length;
            return table[slot];
        }
    }
}