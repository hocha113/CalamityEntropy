using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class NihilityFragments : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 120;
            ItemID.Sets.SortingPriorityMaterials[Type] = 98;
        }

        public override void SetDefaults() {
            Item.width = 42;
            Item.height = 42;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }
    }
}
