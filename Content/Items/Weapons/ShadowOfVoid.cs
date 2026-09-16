using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class ShadowOfVoid : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 2480;
            Item.DamageType = DamageClass.Magic;
            Item.width = 40;
            Item.height = 50;
            Item.useTime = 120;
            Item.useAnimation = 120;
            Item.knockBack = 4;
            Item.UseSound = new("CalamityEntropy/Assets/Sounds/DevourerDeathImpact") { Volume = 0.4f, Pitch = 0.1f };
            Item.shoot = ModContent.ProjectileType<CruiserShadow>();
            Item.shootSpeed = 6f;
            Item.mana = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override bool MagicPrefix() {
            return true;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidAnnihilate>())
                .AddIngredient(ModContent.ItemType<Silence>())
                .AddIngredient(ModContent.ItemType<VoidEcho>())
                .AddIngredient(ModContent.ItemType<WingsOfHush>())
                .AddIngredient(ModContent.ItemType<VoidRelics>())
                .AddIngredient(ModContent.ItemType<FadingRunestone>())
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();

        }
    }
}
