using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.Core
{
    /// <summary>
    /// 策划定死的顺序 C A C B C D,一二阶段同表,没有随机也没有替补
    /// 转阶段时序号归零,三阶段不走表
    /// 只权威端调
    /// </summary>
    public static class SandseaRotation
    {
        private static readonly SandseaStateIndex[] Table =
        {
            SandseaStateIndex.LeapChase, SandseaStateIndex.LeapDash, SandseaStateIndex.LeapChase,
            SandseaStateIndex.SharkBarrage, SandseaStateIndex.LeapChase, SandseaStateIndex.BeetleBombs,
        };

        public static SandseaStateBase Create(SandseaStateIndex state)
            => VaultStateRegistry<SandseaStateContext>.Create((int)state) as SandseaStateBase;

        /// <summary>取表里当前一格并推进序号</summary>
        public static SandseaStateIndex Pick(SandseaStateContext ctx) {
            int slot = ((ctx.AttackIndex % Table.Length) + Table.Length) % Table.Length;
            ctx.AttackIndex = (slot + 1) % Table.Length;
            return Table[slot];
        }
    }
}
