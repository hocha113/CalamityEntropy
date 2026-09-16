using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Core.Cooldowns
{
    /// <summary>
    /// 冷却静态门面。与 Common/CECooldowns(本地玩家短冷却工具,无 UI)是两套系统,勿混用。
    /// </summary>
    public static class CECooldown
    {
        /// <summary>给玩家添加冷却,时长自动应用 CooldownTimeMult 倍率。</summary>
        public static CECooldownInstance Add(Player player, string id, int duration, bool overwrite = true)
            => player.GetModPlayer<CECooldownPlayer>().Add(id, duration, overwrite);

        public static bool Has(Player player, string id)
            => player.GetModPlayer<CECooldownPlayer>().Has(id);

        public static bool TryGet(Player player, string id, out CECooldownInstance instance)
            => player.GetModPlayer<CECooldownPlayer>().TryGet(id, out instance);

        public static bool Remove(Player player, string id)
            => player.GetModPlayer<CECooldownPlayer>().Remove(id);

        public static void Clear(Player player)
            => player.GetModPlayer<CECooldownPlayer>().Clear();
    }

    /// <summary>
    /// Player / Item 扩展,形状对齐原灾厄 CalamityUtils 冷却扩展:
    /// 调用点只需把灾厄的 using 换成 CalamityEntropy.Core.Cooldowns,写法不变。
    /// 注意不要与灾厄的 using 同文件共存,否则 AddCooldown / HasCooldown 二义。
    /// </summary>
    public static class CECooldownExtensions
    {
        /// <summary>取玩家冷却容器,替代灾厄玩家访问器的冷却用途。GetModPlayer 不会静默失败。</summary>
        public static CECooldownPlayer EntropyCooldowns(this Player player)
            => player.GetModPlayer<CECooldownPlayer>();

        /// <summary>对齐灾厄 player.AddCooldown(id, duration, overwrite)。</summary>
        public static CECooldownInstance AddCooldown(this Player player, string id, int duration, bool overwrite = true)
            => player.GetModPlayer<CECooldownPlayer>().Add(id, duration, overwrite);

        /// <summary>对齐灾厄 player.HasCooldown(id)。</summary>
        public static bool HasCooldown(this Player player, string id)
            => player.GetModPlayer<CECooldownPlayer>().Has(id);

        public static bool TryGetCooldown(this Player player, string id, out CECooldownInstance instance)
            => player.GetModPlayer<CECooldownPlayer>().TryGet(id, out instance);

        public static bool RemoveCooldown(this Player player, string id)
            => player.GetModPlayer<CECooldownPlayer>().Remove(id);

        public static void ClearCooldowns(this Player player)
            => player.GetModPlayer<CECooldownPlayer>().Clear();

        /// <summary>对齐灾厄 player.GetDisplayedCooldowns(),冷却栏 UI 数据源。</summary>
        public static IList<CECooldownInstance> GetDisplayedCooldowns(this Player player)
            => player.GetModPlayer<CECooldownPlayer>().GetDisplayed();

        /// <summary>取每玩家具名充能计量器,不存在则按 max 创建。</summary>
        public static CEChargeMeter GetChargeMeter(this Player player, string key, float max)
            => player.GetModPlayer<CECooldownPlayer>().GetCharge(key, max);

        /// <summary>取每物品充能计量器,不存在则按 max 创建。已存在时同步 Max 到最新值。</summary>
        public static CEChargeMeter GetChargeMeter(this Item item, float max) {
            var global = item.GetGlobalItem<CEChargeGlobalItem>();
            if (global.meter == null)
                global.meter = new CEChargeMeter(max);
            else
                global.meter.Max = max;
            return global.meter;
        }
    }
}
