using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class SacrificalMask : ModItem
    {

        public override void SetDefaults() {
            Item.width = 86;
            Item.height = 86;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.Entropy().stroke = true;
            Item.Entropy().strokeColor = Color.Red;
            Item.Entropy().NameColor = Color.Black;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SacrificalDagger>()] < 8) {
                // 脱离灾厄:灾厄 AverageDamageClass 收敛为通用伤害(player-api.md §5)
                Projectile.NewProjectile(player.GetSource_FromAI(), player.Center, Vector2.Zero, ModContent.ProjectileType<SacrificalDagger>(), (int)player.GetTotalDamage(DamageClass.Generic).ApplyTo(40.ApplyAccArmorDamageBonus(player)), 1, player.whoAmI);
            }
            player.Entropy().sacrMask = true;
        }

        public override void AddRecipes() {
        }
    }
}
