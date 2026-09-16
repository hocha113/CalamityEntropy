using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class HellIndustrialComponents : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.SortingPriorityMaterials[Type] = 16;
            ItemID.Sets.ExtractinatorMode[Item.type] = Item.type;
        }

        public override void SetDefaults() {
            Item.value = Item.sellPrice(silver: 4);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.maxStack = 9999;
            Item.MakeUsableWithChlorophyteExtractinator();
            Item.useTime = 2;
        }
        public override void ExtractinatorUse(int extractinatorBlockType, ref int resultType, ref int resultStack) {
            float dropRand = Main.rand.NextFloat();
            resultStack = Main.rand.Next(2, 4);

            // 产出：40% 阿扎弗电路 / 40% 阿扎弗镀层 / 20% 5-12 枚银币
            if (dropRand < 0.4f)
                resultType = ModContent.ItemType<AzafureCircuitry>();
            else if (dropRand < 0.8f)
                resultType = ModContent.ItemType<AzafurePlating>();
            else {
                resultStack = Main.rand.Next(5, 13);
                resultType = ItemID.SilverCoin;
            }
        }
    }
}
