using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class AuraCard : ModItem
    {

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetCritChance(DamageClass.Generic) += 5;

            player.GetModPlayer<EModPlayer>().auraCard = true;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.IronCrate, 5)
                .AddTile(TileID.WorkBenches).DisableDecraft().Register();
            CreateRecipe().AddIngredient(ItemID.IronCrateHard, 5)
                .AddTile(TileID.WorkBenches).DisableDecraft().Register();
        }
    }
}
