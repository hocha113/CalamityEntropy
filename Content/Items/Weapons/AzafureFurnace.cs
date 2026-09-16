using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureFurnace : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults() {
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.damage = 40;
            Item.DamageType = DamageClass.Magic;
            Item.useTime = 3;
            Item.useAnimation = 3;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AzafureFurnaceHoldout>();
            Item.knockBack = 8f;
            Item.value = Item.buyPrice(0, 2);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.shootSpeed = 25f;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.mana = 15;
        }
        public override void AddRecipes() {
            CreateRecipe().
                AddIngredient<OverloadFurnace>().
                AddIngredient(ItemID.Nanites, 100).
                AddTile(TileID.MythrilAnvil).
                Register();
        }

        public override bool MagicPrefix() {
            return true;
        }
    }
}
