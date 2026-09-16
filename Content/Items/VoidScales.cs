using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class VoidScales : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 120;
            ItemID.Sets.SortingPriorityMaterials[Type] = 112;
        }

        public override void SetDefaults() {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 45);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }
}
