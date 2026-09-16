using CalamityEntropy.Common;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class TemperanceCard : ModItem
    {
        public static int MinionsAddition = 1;
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.maxMinions += MinionsAddition;
            player.GetModPlayer<EModPlayer>().temperanceCard = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[T]", MinionsAddition);
        }
        public override void AddRecipes() {
        }
    }
}
