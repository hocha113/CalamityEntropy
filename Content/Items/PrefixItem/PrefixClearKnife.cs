
using CalamityEntropy.Content.ArmorPrefixes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.PrefixItem
{
    public class PrefixClearKnife : ModItem
    {
        public override void SetDefaults() {
            Item.width = Item.height = 46;
            Item.rare = ItemRarityID.Yellow;
        }
        public override bool ConsumeItem(Player player) {
            return false;
        }
        public override void AddRecipes() {
            if (ArmorPrefix.Enabled) {
                CreateRecipe().AddIngredient(ItemID.GoldBar, 2)
                    .AddIngredient(ItemID.Ruby)
                    .Register();

                CreateRecipe().AddIngredient(ItemID.PlatinumBar, 2)
                    .AddIngredient(ItemID.Ruby)
                    .Register();
            }
        }
    }
}