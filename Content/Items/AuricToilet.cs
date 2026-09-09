using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items
{
    public class AuricToilet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 30;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<AToilet>();
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
        }

        public override void AddRecipes()
        {
            // 三把灾厄主题椅换为原版奇珍椅，保持“三椅合一”的配方趣味；门槛由虚空锭把关
            CreateRecipe().
                AddCalOrOwn(CEID.Item_BotanicChair, ItemID.GoldenChair).
                AddCalOrOwn(CEID.Item_CosmiliteChair, ItemID.LihzahrdChair).
                AddCalOrOwn(CEID.Item_SilvaChair, ItemID.MartianHoverChair).
                AddCalOrOwn(CEID.Item_AuricBar, ModContent.ItemType<VoidBar>(), 5).
                AddCalTileOrOwn(CEID.Tile_CosmicAnvil, TileID.LunarCraftingStation).
                Register();
        }
    }
}
