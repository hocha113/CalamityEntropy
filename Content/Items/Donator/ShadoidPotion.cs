using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class ShadoidPotion : ModItem, IDonatorItem
    {
        public string DonatorName => "玲瓏";
        public override void SetStaticDefaults() {
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(3, 16));
        }
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.useTurn = true;
            Item.maxStack = 1;
            Item.healLife = 30;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.potion = true;
            Item.consumable = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 6, 50, 0);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            for (int i = 0; i < tooltips.Count; i++) {
                if (tooltips[i].Name == "Consumable") {
                    tooltips.RemoveAt(i);
                    break;
                }
            }
        }
        public override bool ConsumeItem(Player player) {
            return false;
        }
        public override bool? UseItem(Player player) {
            player.wingTime = player.wingTimeMax;
            return true;
        }
        public override bool CanUseItem(Player player) {
            return !player.HasBuff(BuffID.PotionSickness);
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.HealingPotion, 1).AddIngredient(ItemID.Gel, 16).AddIngredient(ItemID.Sunflower, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
