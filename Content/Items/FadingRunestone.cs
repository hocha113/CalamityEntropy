using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items
{
    public class FadingRunestone : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 10;
            ItemID.Sets.SortingPriorityMaterials[Type] = 160;
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 5));
        }
        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(gold: 60);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.maxStack = 9999;
        }


        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            float brightness = Main.essScale * Main.rand.NextFloat(0.7f, 1.4f);
            Lighting.AddLight(Item.Center, 0.7f * brightness, 0.7f * brightness, 2f * brightness);
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ExoPrism, CEID.Item_AshesofAnnihilation))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_ExoPrism, 5)
                .AddIngredient(CEID.Item_AshesofAnnihilation, 5)
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>()
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<VoidBar>(9999)
                .AddTile<VoidWellTile>()
                .Register();
        }
    }
}
