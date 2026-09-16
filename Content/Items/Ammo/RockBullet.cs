using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Ammo
{
    public class RockBullet : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 20;
        }

        public override void SetDefaults() {
            Item.damage = 1;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 8;
            Item.height = 8;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.knockBack = 1f;
            Item.value = 0;
            Item.rare = ItemRarityID.Gray;
            Item.shoot = ModContent.ProjectileType<RockBulletShot>();
            Item.shootSpeed = 4f;
            Item.ammo = AmmoID.Bullet;
        }

        public override void AddRecipes() {
            CreateRecipe(5)
                .AddIngredient(ItemID.StoneBlock)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
