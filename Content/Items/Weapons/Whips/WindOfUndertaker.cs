using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class WindOfUndertaker : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CruiserWhipDebuff.TagDamage);

        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<WindOfUndertakerProjectile>(), 235, 2, 8, 36);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
        }
        public override void AddRecipes() {
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override bool MeleePrefix() {
            return true;
        }
    }
}
