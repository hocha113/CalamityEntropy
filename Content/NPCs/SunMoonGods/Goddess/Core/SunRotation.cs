using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core
{
    /// <summary>
    /// 每阶段一张固定表按序循环,相邻两手不同类,没有随机也没有替补
    /// 冲刺像原版女皇那样穿插在弹幕招之间,转阶段时序号归零,表头就是该阶段的签名招
    /// 只权威端调
    /// </summary>
    public static class SunRotation
    {
        private static readonly SunStateIndex[] Phase1 =
        {
            SunStateIndex.BirdFlock, SunStateIndex.SolarDash, SunStateIndex.CoronaDance,
            SunStateIndex.LanceHunt, SunStateIndex.SolarDash, SunStateIndex.FlareBloom,
        };

        private static readonly SunStateIndex[] Phase2 =
        {
            SunStateIndex.Sunfall, SunStateIndex.SolarDash, SunStateIndex.LanceWall, SunStateIndex.BirdFlock,
            SunStateIndex.CoronaDance, SunStateIndex.SolarDash, SunStateIndex.LanceHunt, SunStateIndex.FlareBloom,
        };

        private static readonly SunStateIndex[] Phase3 =
        {
            SunStateIndex.SolarStorm, SunStateIndex.SolarDash, SunStateIndex.Sunfall, SunStateIndex.BirdFlock, SunStateIndex.LanceWall,
            SunStateIndex.SolarDash, SunStateIndex.CoronaDance, SunStateIndex.LanceHunt, SunStateIndex.FlareBloom, SunStateIndex.SolarDash,
        };

        public static SunStateIndex[] TableFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        public static SunStateBase Create(SunStateIndex state)
            => VaultStateRegistry<SunStateContext>.Create((int)state) as SunStateBase;

        /// <summary>取表里当前一格并推进序号</summary>
        public static SunStateIndex Pick(SunStateContext ctx) {
            SunStateIndex[] table = TableFor(ctx.Phase);
            int slot = ((ctx.AttackIndex % table.Length) + table.Length) % table.Length;
            ctx.AttackIndex = (slot + 1) % table.Length;
            return table[slot];
        }
    }
}