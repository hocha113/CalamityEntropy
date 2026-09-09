using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class ShadewindLance : ModItem, ICEChargeWeapon
    {
        // 命中计数 8；原潜伏乘数 伤害1.2/弹速1.2/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(8, 1.2f, 1.2f, 3f);

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 34;
            Item.damage = 3000;
            Item.ArmorPenetration = 50;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 28;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<ShadewindLanceThrow>();
            Item.shootSpeed = 46f;
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (CEChargeWeapon.TryConsume(player, Item))
            {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (p >= 0 && p < Main.maxProjectiles)
                {
                    CEChargeWeapon.Empower(p);
                }
                p.ToProj().extraUpdates += 1;
                p.ToProj().netUpdate = true;
                p.ToProj().penetrate = -1;
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_PhantasmalRuin, ItemID.DayBreak)
                .AddIngredient(ModContent.ItemType<VoidBar>(), 5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
