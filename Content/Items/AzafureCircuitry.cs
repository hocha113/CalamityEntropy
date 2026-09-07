using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    /// <summary>
    /// 阿扎弗电路:前期地狱工业材料,承接原灾厄 MysteriousCircuitry / EnergyCore(material-map §一)。
    /// 获取:地狱工业组件提炼产出、地狱工业结构宝箱。
    /// </summary>
    public class AzafureCircuitry : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.SortingPriorityMaterials[Type] = 17;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 18;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 3);
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }
    }
}
