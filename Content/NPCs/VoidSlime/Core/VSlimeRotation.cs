using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.VoidSlime.Core
{
    /// <summary>
    /// 每个形态一张固定表按序循环,顺序照设计稿,没有随机也没有替补
    /// 转形态时序号归零;死兆星阶段四段顺次衔接,不经过这里
    /// 只权威端调
    /// </summary>
    public static class VSlimeRotation
    {
        private static readonly VSlimeStateIndex[] Pale =
        {
            VSlimeStateIndex.PaleHop, VSlimeStateIndex.PaleTeleportSlam, VSlimeStateIndex.PaleSmash,
        };

        //设计稿的 1 2 3 4 5 2 3 4 5
        private static readonly VSlimeStateIndex[] Void =
        {
            VSlimeStateIndex.GravityRings,
            VSlimeStateIndex.StellarBlast, VSlimeStateIndex.BlackHoleBurst, VSlimeStateIndex.GelSpray, VSlimeStateIndex.GloomStarRain,
            VSlimeStateIndex.StellarBlast, VSlimeStateIndex.BlackHoleBurst, VSlimeStateIndex.GelSpray, VSlimeStateIndex.GloomStarRain,
        };

        //设计稿的 2 5 4 1 3
        private static readonly VSlimeStateIndex[] Singularity =
        {
            VSlimeStateIndex.GravityFusion, VSlimeStateIndex.Supercluster, VSlimeStateIndex.GalaxySmash,
            VSlimeStateIndex.StellarBlast, VSlimeStateIndex.EndlessDevour,
        };

        public static VSlimeStateIndex[] TableFor(int phase)
            => phase >= VSlimeForm.Singularity ? Singularity : phase >= VSlimeForm.Void ? Void : Pale;

        public static VSlimeStateBase Create(VSlimeStateIndex state)
            => VaultStateRegistry<VSlimeStateContext>.Create((int)state) as VSlimeStateBase;

        /// <summary>取表里当前一格并推进序号</summary>
        public static VSlimeStateIndex Pick(VSlimeStateContext ctx) {
            VSlimeStateIndex[] table = TableFor(ctx.Phase);
            int slot = ((ctx.AttackIndex % table.Length) + table.Length) % table.Length;
            ctx.AttackIndex = (slot + 1) % table.Length;
            return table[slot];
        }
    }
}
