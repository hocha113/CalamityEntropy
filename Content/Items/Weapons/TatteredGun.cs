using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class TatteredGun : ModItem
    {

        public override bool RangedPrefix() {
            return true;
        }
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 20;
            Item.damage = 4;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 1f;
            Item.value = 0;
            Item.rare = ItemRarityID.Gray;
            Item.UseSound = SoundID.Item11;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 4f;
            Item.useAmmo = AmmoID.Bullet;

        }
        public override Vector2? HoldoutOffset() {
            return new Vector2(-8, 0);
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.StoneBlock, 40).AddTile(TileID.WorkBenches).Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {

            if (Main.rand.NextBool(5)) {
                CEUtils.PlaySound("gunshot_small" + Main.rand.Next(1, 4).ToString(), 1, position);
                return false;
            }
            return true;
        }
    }
}
