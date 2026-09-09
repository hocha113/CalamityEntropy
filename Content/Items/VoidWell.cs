using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items
{
    public class VoidWell : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 48;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 12;
            Item.useTime = 12;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<VoidWellTile>();
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_VoidCondenser))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_VoidCondenser)
                .AddIngredient(ModContent.ItemType<VoidScales>(), 10)
                .AddIngredient(ItemID.FragmentVortex, 6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.AdamantiteForge)
                .AddIngredient<VoidScales>(10)
                .AddIngredient(ItemID.LunarBar, 20)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
