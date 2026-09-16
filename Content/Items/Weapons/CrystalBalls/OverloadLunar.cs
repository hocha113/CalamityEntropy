using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.CrystalBalls
{
    public class OverloadLunar : ModItem
    {
        public override void SetDefaults() {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 75;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.knockBack = 4f;
            Item.UseSound = CEUtils.GetSound("soulshine");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(1, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<OverloadLunarHoldout>();
            Item.shootSpeed = 16f;
            Item.mana = 2;
            Item.DamageType = DamageClass.Magic;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.CrystalBall)
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
        public override bool MagicPrefix() {
            return true;
        }
    }
}
