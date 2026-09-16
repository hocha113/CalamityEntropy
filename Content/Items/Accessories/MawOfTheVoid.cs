using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class MawOfTheVoid : ModItem
    {
        public static int Damage = 100;
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().mawOfVoid = true;
        }

        public override void AddRecipes() {
        }
    }
}
