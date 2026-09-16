using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class ApsychosRelic : ModItem
    {
        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<ApsychosRelicTile>(), 0);

            Item.width = 48;
            Item.height = 62;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.buyPrice(0, 5);
        }
    }
}
