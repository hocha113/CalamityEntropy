using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AbyssalPiercer : ModItem, ICEChargeWeapon
    {
        // 命中计数 8；原潜伏乘数 伤害1.2/弹速1.5/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(8, 1.2f, 1.5f, 3f);

        public override void SetDefaults() {
            Item.width = 42;
            Item.height = 42;
            Item.damage = 60;
            Item.ArmorPenetration = 12;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.ArmorPenetration = 86;
            Item.knockBack = 6f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<AbyssalPiercerThrow>();
            Item.shootSpeed = 40f;
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (CEChargeWeapon.TryConsume(player, Item)) {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (p >= 0 && p < Main.maxProjectiles) {
                    p.ToProj().penetrate = 5;
                    CEChargeWeapon.Empower(p);
                }
                return false;
            }
            return true;
        }
    }
}
