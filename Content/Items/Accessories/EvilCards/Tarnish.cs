using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.EvilCards
{
    public class Tarnish : ModItem
    {
        public static int BlackFireDamage = 25;
        public static int BlackFireCooldownMin = 8;

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<EModPlayer>().TarnishCard = true;
        }

        public override void AddRecipes() {
        }
    }
}
