using CalamityEntropy.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Lores
{
    /// <summary>
    /// 原生 Lore 物品基类，接替灾厄 LoreItem。
    /// 行为：物品无重力、满亮度绘制；按住 Shift 隐藏常规提示并显示传记全文
    /// （文本取物品本地化键 Lore）；注册了 <see cref="LoreEffect"/> 的物品可正常使用、
    /// 也可在背包中右键，以开关对应效果（开关本体与音效由 LoreReworkSystem 通道统一处理）。
    /// </summary>
    public abstract class CELoreItem : ModItem
    {
        // 3.33 继承灾厄 LoreItem 时分类就是 Items.Lore，三语文案至今仍挂在该分类下；
        // 换成自有基类后若落回 ModItem 默认的 Items，六件传记的名字与正文会全部变成孤儿键
        public override string LocalizationCategory => "Items.Lore";

        /// <summary>本物品是否挂有 LoreEffect（即走 LoreReworkSystem 开关通道）。</summary>
        public bool HasLoreEffect => LoreReworkSystem.loreEffects != null && LoreReworkSystem.loreEffects.ContainsKey(Type);

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;

        // 原灾厄基类恒返回 false，本模组靠已删除的 CanUseItem 钩子放行；
        // 此处以等价原生逻辑接管：效果系统开启且本物品注册了 LoreEffect 时可用，
        // 使用后的开关切换由 LoreReworkItem.UseItem 完成
        public override bool CanUseItem(Player player) => LoreEffect.Enabled && HasLoreEffect;

        // 背包右键同样可开关效果，且不消耗物品
        public override bool CanRightClick() => LoreEffect.Enabled && HasLoreEffect;

        public override bool ConsumeItem(Player player) => false;

        public override void RightClick(Player player)
        {
            // 与 LoreReworkItem.UseItem 保持同一条开关路径
            LoreReworkSystem.ToggleLore(Item);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                Main.LocalPlayer.Entropy().SyncPlayer(-1, Main.myPlayer, false);

            LoreEffect effect = LoreReworkSystem.loreEffects[Type];
            if (effect.useSound.HasValue)
                SoundEngine.PlaySound(LoreReworkSystem.Enabled(Type) ? effect.useSound.Value : CEUtils.GetSound("AscendantOff"), player.Center);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                // Shift：隐藏常规提示行，显示传记全文
                tooltips.RemoveAll(line => line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
                tooltips.Add(new TooltipLine(Mod, "CalamityEntropy:Lore", this.GetLocalizedValue("Lore")));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "CalamityEntropy:LoreHint", Language.GetTextValue("Mods.CalamityEntropy.LoreHoldShift")));
            }
        }
    }
}
