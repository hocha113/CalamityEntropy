using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Potions
{
    public class VoidManaPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 30;
            ItemID.Sets.DrinkParticleColors[Type] = new Color[3]
            {
                new Color(0, 255, 250),
                new Color(40, 140, 220),
                new Color(20, 50, 140)
            };
        }

        public override void SetDefaults()
        {
            Item.DefaultToFood(24, 32, 0, 0, true);
            Item.healMana = 350;
            Item.value = Item.sellPrice(silver: 10);
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }

        public override void AddRecipes()
        {
            CreateRecipe(15)
                .AddIngredient(ItemID.SuperManaPotion, 15)
                .AddIngredient(ItemID.LunarBar)
                .AddTile(TileID.Bottles)
                .DisableDecraft()
                .Register();
        }
    }
}
