using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class AmuletOfSanctuary : ModItem
    {
        public override void SetDefaults() {
            Item.width = 52;
            Item.height = 28;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
        }
        public static string ID => "SanctuaryAmulet";
        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().addEquip(ID);
            player.GetDamage(DamageClass.Summon) += 0.08f;
        }
        public static int GetDefence(int slots) {
            return int.Min(2 * slots, 12);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[A]", GetDefence(Main.LocalPlayer.maxMinions));
        }
        public override void AddRecipes() {
            CreateRecipe().
                AddIngredient(ItemID.ManaCrystal).
                AddIngredient(ItemID.Granite, 20).
                AddIngredient(ItemID.Sapphire, 4).
                Register();
        }
    }
}
