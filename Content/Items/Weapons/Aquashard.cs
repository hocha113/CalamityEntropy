using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Aquashard : ModItem, ICEChargeWeapon
    {
        // 命中计数 6；原潜伏乘数 伤害0.8/弹速1.2/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(6, 0.8f, 1.2f, 3f);

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 34;
            Item.damage = 17;
            Item.ArmorPenetration = 6;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 5f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<AquashardThrow>();
            Item.shootSpeed = 50f;
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (CEChargeWeapon.TryConsume(player, Item))
            {
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
            if (CECal.CalChainReady(CEID.Item_PearlShard, CEID.Item_SeaPrism))
            {
                CreateRecipe().AddIngredient(CEID.Item_PearlShard, 4)
                .AddIngredient(CEID.Item_SeaPrism, 8)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Sapphire, 10)
                .AddIngredient(ItemID.Coral, 10)
                .AddIngredient(ItemID.WhitePearl, 2)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
