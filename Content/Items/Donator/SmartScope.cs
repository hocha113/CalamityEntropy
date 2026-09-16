using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class SmartScope : ModItem, IDonatorItem
    {
        public static NPC target = null;
        public string DonatorName => "a3a4";
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
        }
        public static string ID = "SmartScope";

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().addEquip(ID, !hideVisual);
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Wire, 10)
                .AddIngredient(ItemID.MechanicalLens)
                .AddIngredient(ItemID.Nanites)
                .AddIngredient(ItemID.Glass)
                .AddIngredient(ItemID.RifleScope)
                .AddIngredient(ItemID.TitaniumBar, 4)
                .Register();
        }
    }
}
