using CalamityEntropy.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class LashingBramblerod : BaseWhipItem
    {
        public override int TagDamage => 10;
        public override float TagCritChance => 0.05f;
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            base.ModifyTooltips(tooltips);
            tooltips.Replace("[1]", TagCritChance.ToPercent());
            tooltips.Replace("[2]", SilvaVine.baseDR.ToPercent());
            tooltips.Replace("[3]", SilvaVine.DREachFlower.ToPercent());
            tooltips.Replace("[4]", SilvaVine.RegenPerFlower);
            tooltips.Replace("[5]", SilvaVine.MaxFlowers);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            float swingDirection = 0.6f + (0.4f * Main.rand.NextFloat());
            if (Main.rand.NextBool(3)) {
                swingDirection *= -2.5f;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, swingDirection);
            return false;
        }
        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<LashingBramblerodProjectile>(), 60, 3, 4, 40);
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.buyPrice(gold: 60);
            Item.autoReuse = true;
        }
    }
}
