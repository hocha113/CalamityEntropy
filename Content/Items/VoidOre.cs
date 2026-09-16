using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class VoidOre : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 100;
            ItemID.Sets.SortingPriorityMaterials[Type] = 119;
        }

        public override void SetDefaults() {
            Item.width = 10;
            Item.height = 10;
            Item.createTile = ModContent.TileType<VoidOreTile>();
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 4);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }
}
