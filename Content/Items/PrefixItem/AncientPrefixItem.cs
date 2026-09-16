using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.PrefixItem
{

    public abstract class AncientPrefixItem : BasePrefixItem
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.width = Item.height = 46;
            Item.rare = ItemRarityID.Red;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            base.ModifyTooltips(tooltips);
            tooltips.Add(new TooltipLine(Mod, "Desc", Mod.GetLocalization("AncientPrefixDesc").Value) { OverrideColor = Color.SkyBlue });
        }
    }
}