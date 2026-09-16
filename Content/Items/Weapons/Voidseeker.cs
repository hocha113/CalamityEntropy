using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Voidseeker : ModItem, ICEChargeWeapon
    {
        // 命中计数 8；原潜伏乘数 伤害2/弹速1/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(8, 2f, 1f, 3f);

        public override void SetDefaults() {
            Item.width = 36;
            Item.height = 34;
            Item.damage = 725;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = ModContent.RarityType<NihilityBlue>();
            Item.shoot = ModContent.ProjectileType<VoidseekerProj>();
            Item.shootSpeed = 10f;
            // 实测反馈改判:镰刀挥砍归近战(原并入原版时曾判远程),弹幕侧同步
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (CEChargeWeapon.TryConsume(player, Item)) {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (p >= 0 && p < Main.maxProjectiles) {
                    CEChargeWeapon.Empower(p);
                }
                return false;
            }
            return true;
        }
    }
}
