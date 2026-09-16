using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class TectonicShard : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 99;
            ItemID.Sets.SortingPriorityMaterials[Type] = 56;
        }

        public override void SetDefaults() {
            Item.width = 34;
            Item.height = 38;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.LightRed;
        }
    }
}
