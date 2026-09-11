using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.CrystalBalls
{
    public class NightProjection : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 70;
            Item.crit = 10;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.knockBack = 6f;
            Item.UseSound = CEUtils.GetSound("soulshine");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 80);
            Item.rare = ItemRarityID.Cyan;
            Item.shoot = ModContent.ProjectileType<NightProjectionHoldout>();
            Item.shootSpeed = 22f;
            Item.mana = 2;
            Item.DamageType = DamageClass.Magic;
        }
        public override void AddRecipes()
        {
            // 3.33 没有配方,唯一来源是白金星舰的宝藏袋(已在 EGlobalItem 的灾厄宝袋段补回)。
            // 这条是脱灾期的补偿合成,装灾厄时整条不注册
            if (CERef.Has)
            {
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.CrystalBall)
                .AddIngredient(ItemID.MartianConduitPlating, 30)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        public override bool MagicPrefix()
        {
            return true;
        }
    }
}
