using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.VanillaTweaks
{
    /// <summary>和谐杖禁用与提示、混沌传送杖提示、月亮领主宝藏袋夜明矿翻倍并去掉天界星盘、天顶剑面板与提示、天界星盘改为红色普通物品</summary>
    public class VanillaItemRulesGlobalItem : GlobalItem
    {
        //天顶剑面板伤害,原版 190
        public const int ZenithDamage = 240;

        public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
            return entity.type == ItemID.RodOfHarmony || entity.type == ItemID.RodofDiscord || entity.type == ItemID.MoonLordBossBag
                || entity.type == ItemID.Zenith || entity.type == ItemID.LongRainbowTrailWings;
        }

        public override void SetDefaults(Item item) {
            if (item.type == ItemID.Zenith) {
                item.damage = ZenithDamage;
            }
            else if (item.type == ItemID.LongRainbowTrailWings) {
                //天界星盘改由配方合成,不再是专家物品
                item.rare = ItemRarityID.Red;
                item.expert = false;
            }
        }

        public override bool CanUseItem(Item item, Player player) {
            return !(item.type == ItemID.RodOfHarmony && PostMoonLordBoss.IsFighting());
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            string key = item.type switch {
                ItemID.RodOfHarmony => "Mods.CalamityEntropy.PostMoonLordRodHarmony",
                ItemID.RodofDiscord => "Mods.CalamityEntropy.PostMoonLordRodDiscord",
                ItemID.Zenith => "Mods.CalamityEntropy.ZenithIgnoreDefense",
                _ => null
            };
            if (key == null) {
                return;
            }
            TooltipLine line = new TooltipLine(Mod, "PostMoonLordRule", Language.GetTextValue(key));
            if (item.type == ItemID.RodOfHarmony) {
                line.OverrideColor = new Color(180, 110, 255);
            }
            //天顶剑没有原版描述行,退到材料 / 击退行之后
            int last = tooltips.FindLastIndex(tip => tip.Mod == "Terraria" && (tip.Name.StartsWith("Tooltip") || tip.Name == "Material" || tip.Name == "Knockback"));
            if (last < 0) {
                tooltips.Add(line);
            }
            else {
                tooltips.Insert(last + 1, line);
            }
        }

        public override void ModifyItemLoot(Item item, ItemLoot itemLoot) {
            if (item.type != ItemID.MoonLordBossBag) {
                return;
            }
            itemLoot.RemoveWhere(r => r is CommonDrop starboard && starboard.itemId == ItemID.LongRainbowTrailWings);
            List<CommonDrop> ores = new List<CommonDrop>();
            foreach (IItemDropRule rule in itemLoot.Get()) {
                if (rule is CommonDrop drop && drop.itemId == ItemID.LunarOre) {
                    ores.Add(drop);
                }
            }
            foreach (CommonDrop old in ores) {
                itemLoot.Remove(old);
                itemLoot.Add(new CommonDrop(ItemID.LunarOre, old.chanceDenominator, old.amountDroppedMinimum * 2, old.amountDroppedMaximum * 2, old.chanceNumerator));
            }
        }
    }
}
