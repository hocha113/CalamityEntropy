using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items
{
    public class WyrmTooth : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 120;
            ItemID.Sets.SortingPriorityMaterials[Type] = 112;
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 60;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 70);
            Item.rare = CECal.RarityHotPink(ModContent.RarityType<VoidPurple>());
        }
    }
}
