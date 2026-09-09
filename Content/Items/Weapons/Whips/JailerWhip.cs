using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class JailerWhip : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(JailerWhipDebuff.TagDamage);

        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<JailerWhipProjectile>(), 20, 2, 4);
            Item.rare = ItemRarityID.Orange;
            Item.autoReuse = true;
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
            CreateRecipe().AddCalOrOwn(CEID.Item_AncientBoneDust, 2, ItemID.ShadowKey, 1)
                .AddIngredient(ItemID.Chain, 6)
                .AddIngredient(ItemID.Silk, 4)
                .AddIngredient(ItemID.HellstoneBar, 5)
                .AddTile(TileID.Anvils).Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
}
