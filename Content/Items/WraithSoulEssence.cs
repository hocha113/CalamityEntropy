using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    /// <summary>
    /// 幽渊魂髓:月后二阶材料,承接原灾厄 CosmiliteBar / AscendantSpiritEssence / TwistingNether(material-map §一)。
    /// 获取:虚无双子击杀掉落 15–25(原挂深渊亡魂,该 Boss 移除后改挂,见 NihilityActeriophage.ModifyNPCLoot),
    /// 巨龙商店亦有售,抽奖机 p6/p7 池内也有。
    /// </summary>
    public class WraithSoulEssence : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.SortingPriorityMaterials[Type] = 104;
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            ItemID.Sets.ItemNoGravity[Type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 4));
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 52;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 6);
            Item.rare = ModContent.RarityType<AbyssalBlue>();
        }

        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            float brightness = Main.essScale * Main.rand.NextFloat(0.9f, 1.1f);
            Lighting.AddLight(Item.Center, 0.25f * brightness, 0.6f * brightness, 0.7f * brightness);
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AscendantSpiritEssence))
            {
                CreateRecipe()
                    .AddIngredient(CEID.Item_AscendantSpiritEssence)
                    .Register();
                Recipe.Create(CEID.Item_AscendantSpiritEssence)
                    .AddIngredient(Type)
                    .Register();
            }
        }
    }
}
