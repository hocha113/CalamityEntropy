using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class DeusCore : ModItem
    {
        public override void SetDefaults() {
            Item.width = 52;
            Item.height = 52;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ModContent.RarityType<GlowPurple>();
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().deusCore = true;
        }

        public override void AddRecipes() {
        }
    }
}
