using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AmethystFrisbee : ModItem, ICEChargeWeapon
    {
        // 充能条 4 秒；原潜伏乘数 伤害1.6/弹速1.2/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(4f, 1.6f, 1.2f, 3f);

        public override void SetDefaults() {
            Item.width = 54;
            Item.height = 58;
            Item.damage = 21;
            Item.ArmorPenetration = 8;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 34;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.ArmorPenetration = 4;
            Item.knockBack = 4f;
            Item.UseSound = null;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<AmethystFrisbeeProjectile>();
            Item.shootSpeed = 36f;
            Item.DamageType = DamageClass.Ranged;
        }
        public int altShotCount = 0;

        public override void UpdateInventory(Player player) {
            if (altShotCount > 0) {
                altShotCount--;
            }
        }
        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[Item.shoot] <= 0;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (altShotCount > 0) {
                velocity *= 0.54f;
            }
            CEUtils.PlaySound("throw", 1, player.Center);
            if (CEChargeWeapon.TryConsume(player, Item)) {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, altShotCount > 0 ? 1 : 0);
                if (p >= 0 && p < Main.maxProjectiles) {
                    CEChargeWeapon.Empower(p);
                }
                return false;
            }
            else {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, altShotCount > 0 ? 1 : 0);
            }
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.Amethyst, 8)
                .AddIngredient(ItemID.Diamond, 4)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
