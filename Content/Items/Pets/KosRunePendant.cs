using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Projectiles.Pets.DoG;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class KosRunePendant : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<DoG>();
            Item.buffType = ModContent.BuffType<DevourerAndTheApostles>();
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<BottleDarkMatter>())
                .AddIngredient(ModContent.ItemType<LightningPendant>())
                .AddIngredient(ModContent.ItemType<SoulCandle>())
                .AddIngredient((ModContent.ItemType<GodsSnack>()))
                .AddTile(TileID.WorkBenches).Register();
        }
    }
}