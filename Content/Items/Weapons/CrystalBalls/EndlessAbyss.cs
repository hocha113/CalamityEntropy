using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.CrystalBalls
{
    public class EndlessAbyss : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 4));
        }
        public override void SetDefaults() {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 580;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.knockBack = 6f;
            Item.UseSound = CEUtils.GetSound("soulshine");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(3, 20);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<EndlessAbyssHoldout>();
            Item.shootSpeed = 16f;
            Item.mana = 7;
            Item.DamageType = DamageClass.Magic;

        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.CrystalBall, 1)
                .AddIngredient(ModContent.ItemType<VoidBar>(), 5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
        public override bool MagicPrefix() {
            return true;
        }
    }
}
