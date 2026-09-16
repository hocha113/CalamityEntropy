using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class ArmorReforgStation : ModItem
    {
        public override bool IsLoadingEnabled(Mod mod) {
            return false;
        }
        public override void SetDefaults() {
            Item.width = 56;
            Item.height = 56;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.value = Item.buyPrice(gold: 2);
            Item.createTile = ModContent.TileType<ArmorReforgStationTile>();
            Item.rare = ItemRarityID.Green;
        }

        /*public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 40)
                .AddIngredient(ItemID.Wood, 16)
                .AddIngredient(ItemID.Torch, 1)
                .AddIngredient(ItemID.IronBar, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 40)
                .AddIngredient(ItemID.Wood, 16)
                .AddIngredient(ItemID.Torch, 1)
                .AddIngredient(ItemID.LeadBar, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }*/
    }
}
