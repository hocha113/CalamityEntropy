using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class InspirationCard : ModItem
    {

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<EModPlayer>().inspirationCard = true;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.OasisCrate, 5)
                .AddTile(TileID.WorkBenches).DisableDecraft().Register();
            CreateRecipe().AddIngredient(ItemID.OasisCrateHard, 5)
                .AddTile(TileID.WorkBenches).DisableDecraft().Register();
        }
    }
}
