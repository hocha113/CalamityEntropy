using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class EntityCard : ModItem
    {

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetAttackSpeed(DamageClass.Melee) += 0.10f;
            player.GetModPlayer<EModPlayer>().entityCard = true;
        }

        public override void AddRecipes() {
        }
    }
}
