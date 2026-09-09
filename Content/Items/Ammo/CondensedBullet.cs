using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Ammo
{
    public class CondensedBullet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults()
        {
            Item.damage = 6;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 8;
            Item.height = 8;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true; Item.knockBack = 1f;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<CondensedBulletProjectile>();
            Item.shootSpeed = 1f;
            Item.ammo = AmmoID.Bullet;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_PurifiedGel))
            {
                CreateRecipe(50)
                .AddIngredient(CEID.Item_PurifiedGel)
                .AddTile(TileID.WorkBenches)
                .Register();
                return;
            }
            CreateRecipe(50)
                .AddIngredient(ItemID.Gel, 10)
                .AddIngredient(ItemID.SoulofLight, 1)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
