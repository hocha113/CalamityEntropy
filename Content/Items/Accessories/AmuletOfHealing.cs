using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class AmuletOfHealing : ModItem
    {
        public override void SetDefaults() {
            Item.width = 44;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
        }
        public static string ID => "HealingAmulet";
        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().addEquip(ID);
            player.GetDamage(DamageClass.Summon) += 0.08f;
        }
        public static int GetRegen(int slots) {
            return int.Min(slots, 6);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[A]", GetRegen(Main.LocalPlayer.maxMinions) / 2f);
        }
        public override void AddRecipes() {
            CreateRecipe().
                AddIngredient(ItemID.ManaCrystal).
                AddIngredient(ItemID.Marble, 20).
                AddIngredient(ItemID.Ruby, 4).
                Register();
        }
    }
}
