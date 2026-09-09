using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.CrystalBalls
{
    public class GravityGaze : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 20;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.knockBack = 5f;
            Item.UseSound = CEUtils.GetSound("soulshine");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 5);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<GravityGazeHoldout>();
            Item.shootSpeed = 22f;
            Item.mana = 3;
            Item.DamageType = DamageClass.Magic;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AerialiteBar))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_AerialiteBar, 5)
                .AddIngredient(ItemID.Glass, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Glass, 10)
                .AddIngredient(ItemID.Bone, 30)
                .AddIngredient(ItemID.Feather, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        public override bool MagicPrefix()
        {
            return true;
        }
    }
}
