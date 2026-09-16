using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Potions
{
    public class VoidHealingPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 30;
            ItemID.Sets.DrinkParticleColors[Type] = new Color[3]
            {
                new Color(255, 40, 70),
                new Color(180, 20, 80),
                new Color(90, 10, 50)
            };
        }

        public override void SetDefaults()
        {
            Item.DefaultToHealingPotion(24, 32, 350);
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }

        public override void AddRecipes()
        {
            CreateRecipe(4)
                .AddIngredient(ItemID.SuperHealingPotion, 4)
                .AddIngredient(ItemID.LunarBar)
                .AddTile(TileID.Bottles)
                .DisableDecraft()
                .Register();
        }
    }
}
