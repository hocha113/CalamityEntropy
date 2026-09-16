using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.SoulCards
{
    public class GrudgeCard : ModItem
    {
        public static float TempDefense = 6;
        public static int HealAmount = 4;
        public static int TriggerCooldown = 140;
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().grudgeCard = true;
        }

        public override void AddRecipes() {
            Recipe.Create(1508, 12)
                .AddIngredient(Type)
                .Register();
        }
    }
}
