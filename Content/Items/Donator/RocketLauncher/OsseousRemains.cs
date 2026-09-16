using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher
{
    public class OsseousRemains : ModItem, IDonatorItem
    {
        public string DonatorName => "Ovasa";
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.SortingPriorityMaterials[Type] = 12;
        }
        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 22;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 1);
            Item.rare = ItemRarityID.Green;
        }
    }
}
