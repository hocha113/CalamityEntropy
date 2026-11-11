using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon.Loot
{
    public class SandseaDragonTrophy : ModItem
    {
        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<SandseaDragonTrophyTile>());

            Item.width = 30;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: 1); //同原版纪念章
        }
    }
}
