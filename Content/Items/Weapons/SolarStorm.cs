using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class SolarStorm : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(4, 10));
        }
        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 138;
            Item.damage = 340;
            Item.crit = 10;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.channel = true;
            Item.shootSpeed = 16f;
            Item.useAmmo = AmmoID.Arrow;
            Item.noUseGraphic = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SolarStormHeld>(), damage, knockback, player.whoAmI);
            return false;
        }
        public override Vector2? HoldoutOffset() => new Vector2(-28, 0);

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ContinentalGreatbow, CEID.Item_TelluricGlare, CEID.Item_AuricBar, CEID.Tile_CosmicAnvil))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_ContinentalGreatbow)
                .AddIngredient(CEID.Item_TelluricGlare)
                .AddIngredient<Prominence>()
                .AddIngredient(CEID.Item_AuricBar, 5)
                .AddIngredient(ItemID.FragmentSolar, 20)
                .AddIngredient(ItemID.FragmentVortex, 5)
                .AddTile(CEID.Tile_CosmicAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Prominence>()
                .AddIngredient(ItemID.Uzi)
                .AddIngredient<NihilityFragments>(10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
        public override bool RangedPrefix()
        {
            return true;
        }
    }
}
