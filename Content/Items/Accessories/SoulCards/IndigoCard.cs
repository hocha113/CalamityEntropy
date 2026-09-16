using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.SoulCards
{
    public class IndigoCard : ModItem
    {
        public static float WingTimeAddition = 0.3f;
        public static float WingSpeedAddition = 0.08f;
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().WingSpeed += WingSpeedAddition;
            player.Entropy().WingTimeMult += WingTimeAddition;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[T]", WingSpeedAddition.ToPercent());
            tooltips.Replace("[T2]", WingTimeAddition.ToPercent());
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.FloatingIslandFishingCrate, 5).DisableDecraft().Register();
            CreateRecipe().AddIngredient(ItemID.FloatingIslandFishingCrateHard, 5).DisableDecraft().Register();
        }
    }
}
