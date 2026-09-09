using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class ArbitratorII : ModItem, ICEChargeWeapon
    {
        // 周期就绪 8 秒；原潜伏乘数 伤害0.75/弹速1.5/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.Periodic(8f, 0.75f, 1.5f, 3f);

        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.damage = 190;
            Item.noMelee = true;
            Item.crit = 10;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 32;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<ArbitratorIIThrow>();
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
                    CEChargeWeapon.Empower(p);
                }
                return false;
            }
            return true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ScoriaBar, CEID.Item_SolarVeil, CEID.Item_PlagueCellCanister))
            {
                CreateRecipe().AddIngredient(CEID.Item_ScoriaBar, 6)
                .AddIngredient(CEID.Item_SolarVeil, 4)
                .AddIngredient(CEID.Item_PlagueCellCanister, 1)
                .AddTile(TileID.MythrilAnvil).Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.AdamantiteGlaive)
                .AddIngredient(ItemID.MartianConduitPlating, 10)
                .AddIngredient(ItemID.LihzahrdBrick, 50)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
