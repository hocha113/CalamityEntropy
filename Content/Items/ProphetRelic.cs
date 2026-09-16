using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class ProphetRelic : ModItem
    {

        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<ProphetRelicTile>(), 0);

            Item.width = 30;
            Item.height = 40;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.buyPrice(0, 5);
        }
    }
}
