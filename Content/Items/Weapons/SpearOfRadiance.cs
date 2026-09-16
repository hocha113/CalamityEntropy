using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class SpearOfRadiance : ModItem, ICEChargeWeapon
    {
        // 命中计数 6；原潜伏乘数 伤害1.2/弹速1.5/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(6, 1.2f, 1.5f, 3f);

        public override void SetDefaults() {
            Item.width = 36;
            Item.height = 34;
            Item.damage = 92;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 5f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 35);
            Item.rare = ItemRarityID.LightPurple;
            Item.shoot = ModContent.ProjectileType<RadianceSpearThrow>();
            Item.shootSpeed = 68f;
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

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Gungnir)
                .AddIngredient(ItemID.ChlorophyteBar, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
