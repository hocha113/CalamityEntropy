using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.EvilCards
{
    public class GreedCard : ModItem
    {
        public static float DamagePerMinion = 0.015f;
        public static float DamageCap = 0.15f;

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<EModPlayer>().GreedCard = true;
        }

        public override void AddRecipes() {
        }
    }
}
