using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.CrystalBalls
{
    public class DivineRadience : ModItem
    {
        public override void SetDefaults() {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 32;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.knockBack = 5f;
            Item.UseSound = CEUtils.GetSound("soulshine");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 35);
            Item.rare = ItemRarityID.LightPurple;
            Item.shoot = ModContent.ProjectileType<DivineRadienceHoldout>();
            Item.shootSpeed = 16f;
            Item.mana = 10;
            Item.DamageType = DamageClass.Magic;
        }
        public override bool MagicPrefix() {
            return true;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.CrystalBall)
                .AddIngredient(ItemID.SoulofLight, 20)
                .AddIngredient(ItemID.AdamantiteBar, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
