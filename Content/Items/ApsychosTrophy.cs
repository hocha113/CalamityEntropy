using CalamityEntropy.Content.Tiles;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class ApsychosTrophy : ModItem
    {
        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.value = 50000;
            Item.rare = ItemRarityID.LightRed;
            Item.createTile = ModContent.TileType<ApsychosTrophyTile>();
        }
    }
}
