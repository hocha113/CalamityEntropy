using CalamityEntropy.Content.ArmorPrefixes;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.PrefixItem
{

    public abstract class BasePrefixItem : ModItem
    {
        public override void SetDefaults() {
            Item.width = Item.height = 46;
            Item.rare = ItemRarityID.Yellow;
            Item.SetNameOverride(Item.Name.Replace("|", ArmorPrefix.findByName(PrefixName).GivenName));
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            var prefix = ArmorPrefix.findByName(PrefixName);
            //只有 DisplayName 里带这个占位竖线,别拿它去扫全部行:竖线太常见,会误伤别的模组注入的提示行
            foreach (TooltipLine line in tooltips) {
                if (line.Name == "ItemName" && line.Text != null) {
                    line.Text = line.Text.Replace("|", prefix.GivenName);
                }
            }
            foreach (TooltipLine line in tooltips) {
                if (line.Mod == "Terraria") {
                    line.OverrideColor = prefix.getColor();
                }
            }
            tooltips.Add(prefix.getDescTooltipLine());
            tooltips.Add(new TooltipLine(Mod, "Armor Prefix Item Description", Mod.GetLocalization("PrefixitemDesc").Value) { OverrideColor = Color.Yellow });
        }
        public virtual string PrefixName => "";
        public override string Texture => "CalamityEntropy/Content/Items/PrefixItem/Textures/" + PrefixName;
    }
}