using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class EnduranceCard : ModItem
    {

        public override void SetDefaults() {
            Item.width = 22;
            Item.defense = 5;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<EModPlayer>().enduranceCard = true;
        }

        public override void AddRecipes() {
        }
    }
}
