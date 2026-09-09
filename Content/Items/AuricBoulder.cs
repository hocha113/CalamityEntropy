using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items
{
    public class AuricBoulder : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 4);
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.DefaultToPlaceableTile(ModContent.TileType<AuricBoulderTile>(), 0);
            Item.width = 32;
            Item.height = 32;
        }

        public override bool CanShoot(Player player)
        {
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddCalOrOwn(CEID.Item_AuricBar, ModContent.ItemType<VoidBar>(), 1).AddCalTileOrOwn(CEID.Tile_CosmicAnvil, TileID.LunarCraftingStation).Register();
        }
    }
}
