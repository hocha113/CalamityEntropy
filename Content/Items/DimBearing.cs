using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    // 黯淡轴承:虚空驱逐舰掉落材料
    public class DimBearing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.SortingPriorityMaterials[Type] = 111;
        }

        public override void SetDefaults()
        {
            Item.width = 25;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }
}
