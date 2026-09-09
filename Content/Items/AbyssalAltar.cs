using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items
{
    public class AbyssalAltar : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 92;
            Item.height = 50;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 6;
            Item.useTime = 6;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<AbyssalAltarTile>();
            Item.rare = ModContent.RarityType<AbyssalBlue>();
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AltarOfTheAccursedItem))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_AltarOfTheAccursedItem)
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 10)
                .AddTile<VoidWellTile>()
                .Register();
                return;
            }
            // 灾厄诅咒祭坛原料随脱离灾厄移除；门槛由龙牙（巡游者掉落）与虚空井站台把关
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 10)
                .AddTile<VoidWellTile>()
                .Register();
        }
    }
}
