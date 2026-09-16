using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class AcropolisRelic : ModItem
    {
        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<AcropolisRelicTile>(), 0);

            Item.width = 30;
            Item.height = 40;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.buyPrice(0, 5);
        }
    }
}
