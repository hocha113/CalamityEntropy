using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class WhipOfService : ModItem
    {

        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<WhipOfServiceProjectile>(), 24, 2, 4, 36);
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);

        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float swingDirection = 0.6f + (0.4f * Main.rand.NextFloat());
            if (Main.rand.NextBool(3))
            {
                swingDirection *= -2.5f;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, swingDirection);
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AncientBoneDust))
            {
                CreateRecipe().AddIngredient(ItemID.BlandWhip)
                .AddIngredient(CEID.Item_AncientBoneDust, 2)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.BlandWhip)
                .AddIngredient(ItemID.DarkShard, 2)
                .AddIngredient(ItemID.LovePotion)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
}
