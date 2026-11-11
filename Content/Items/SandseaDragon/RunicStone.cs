using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon
{
    /// <summary>符文之石,沙海冥龙掉落的材料</summary>
    public class RunicStone : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
            //原版世纪之花后的材料占 91 到 94(幽灵锭 94),月亮碎片从 95 起,这里与幽灵锭同档
            ItemID.Sets.SortingPriorityMaterials[Type] = 94;
        }

        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 34;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Blue;
        }
    }
}
