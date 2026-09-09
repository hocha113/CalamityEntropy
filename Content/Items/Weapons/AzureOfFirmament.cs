using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzureOfFirmament : ModItem, ICEChargeWeapon
    {
        // 充能条 5 秒；原潜伏乘数 伤害1.2/弹速1/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(5f, 1.2f, 1f, 3f);

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.damage = 40;
            Item.ArmorPenetration = 10;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 1f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<AzureOfFirmamentThrow>();
            Item.shootSpeed = 50f;
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (CEChargeWeapon.TryConsume(player, Item))
            {
                for (int i = 0; i < 12; i++)
                {
                    Projectile.NewProjectile(source, position, CEUtils.randomRot().ToRotationVector2() * 6, ModContent.ProjectileType<WelkinFeather>(), damage / 4, knockback, player.whoAmI, 0f, -1f);
                }
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (p >= 0 && p < Main.maxProjectiles)
                {
                    p.ToProj().penetrate = 5;
                    CEChargeWeapon.Empower(p);
                }
                return false;
            }
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddCalOrOwn(CEID.Item_AerialiteBar, ItemID.MeteoriteBar, 8).
                AddIngredient(ItemID.SunplateBlock, 6).
                AddIngredient(ItemID.Feather, 2).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
