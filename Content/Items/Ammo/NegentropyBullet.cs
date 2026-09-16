using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Ammo
{
    public class NegentropyBullet : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults() {
            Item.damage = 8;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 8;
            Item.height = 8;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true; Item.knockBack = 1f;
            Item.value = 20000;
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<NegentropyBulletProjectile>();
            Item.shootSpeed = 10;
            Item.ammo = AmmoID.Bullet;
        }

        public override void AddRecipes() {
            CreateRecipe(999)
                .AddIngredient<VoidBar>()
                .AddTile<VoidWellTile>()
                .Register();
        }
    }
}
