using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Ammo
{
    public class HiveBullet : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults() {
            Item.damage = 9;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 8;
            Item.height = 8;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true; Item.knockBack = 2f;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<HiveBulletProjectile>();
            Item.shootSpeed = 16.0f;
            Item.ammo = AmmoID.Bullet;
        }

        public override void AddRecipes() {
            CreateRecipe(50)
                .AddIngredient(ItemID.BeeWax)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
