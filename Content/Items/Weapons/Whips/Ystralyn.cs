using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class Ystralyn : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DragonWhipDebuff.TagDamage);
        public static int PhantomDamage = 1600;
        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<YstralynProj>(), 900, 2, 4, 27);
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.autoReuse = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            float swingDirection = 0.6f + (0.4f * Main.rand.NextFloat());
            if (Main.rand.NextBool(3)) {
                swingDirection *= -2.5f;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, swingDirection);
            return false;
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.RainbowWhip)
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 12)
                .AddIngredient(ModContent.ItemType<FadingRunestone>())
                .AddTile(ModContent.TileType<AbyssalAltarTile>())
                .Register();
        }

        public override bool MeleePrefix() {
            return true;
        }
    }
}
