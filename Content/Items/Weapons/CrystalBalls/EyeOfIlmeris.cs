using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.CrystalBalls
{
    public class EyeOfIlmeris : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 14;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.knockBack = 4f;
            Item.UseSound = CEUtils.GetSound("soulshine");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 2);
            Item.rare = ItemRarityID.Green;
            Item.shoot = ModContent.ProjectileType<EyeOfIlmerisHoldout>();
            Item.shootSpeed = 16f;
            Item.mana = 10;
            Item.DamageType = DamageClass.Magic;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_PearlShard, CEID.Item_SeaPrism))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_PearlShard, 2)
                .AddIngredient(CEID.Item_SeaPrism, 5)
                .AddIngredient(ItemID.Glass, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.WhitePearl, 2)
                .AddIngredient(ItemID.Sapphire, 10)
                .AddIngredient(ItemID.Diamond, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        public override bool MagicPrefix()
        {
            return true;
        }
    }
}
