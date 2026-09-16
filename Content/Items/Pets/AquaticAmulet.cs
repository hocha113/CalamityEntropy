using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Projectiles.Pets.Aquatic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class AquaticAmulet : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<AquaticPet>();
            Item.buffType = ModContent.BuffType<AquaticAmuletBuff>();
            Item.UseSound = null;
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ModContent.ItemType<AquaticFlute>()).AddIngredient(ModContent.ItemType<DustyWhistle>())
                .Register();
        }
    }
}