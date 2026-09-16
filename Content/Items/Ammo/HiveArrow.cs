using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Ammo
{
    public class HiveArrow : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults() {
            Item.width = 14;
            Item.height = 36;
            Item.damage = 2;
            Item.DamageType = DamageClass.Ranged;
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.knockBack = 1f;
            Item.value = Item.sellPrice(copper: 26);
            Item.shoot = ModContent.ProjectileType<HiveArrowProjectile>();
            Item.shootSpeed = 0.8f;
            Item.ammo = AmmoID.Arrow;
        }

        public override void AddRecipes() {
            CreateRecipe(50).AddIngredient(ItemID.BeeWax)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
