using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor
{
    /// <summary>键位提示文本扩展(替代灾厄 TooltipHotkeyString)。</summary>
    public static class CEKeybindHint
    {
        /// <summary>返回键位的首个绑定键名,未绑定时返回本地化提示。</summary>
        public static string TooltipKeyHint(this ModKeybind keybind) {
            if (Main.dedServ || keybind is null)
                return "";
            var keys = keybind.GetAssignedKeys();
            if (keys == null || keys.Count == 0)
                return Language.GetOrRegister("Mods.CalamityEntropy.KeyNotBound", () => "未绑定").Value;
            return keys[0];
        }
    }
}
