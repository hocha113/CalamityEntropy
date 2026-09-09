using CalamityEntropy.Content.Projectiles.Chainsaw;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Chainsaw
{
    public class Pioneer : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Melee;
            Item.width = 42;
            Item.height = 42;
            Item.noUseGraphic = true;
            Item.useTime = 16;
            Item.useAnimation = 0;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = 36;
            Item.rare = CECal.RarityCosmicPurple(ModContent.RarityType<AbyssalBlue>());
            Item.UseSound = SoundID.Item23;
            Item.channel = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Pioneer0>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster, CEID.Item_DarksunFragment))
            {
                CreateRecipe().
                AddIngredient<Euangelion>().
                AddIngredient(CEID.Item_ExodiumCluster, 20).
                AddIngredient(CEID.Item_DarksunFragment, 5).
                AddTile(TileID.LunarCraftingStation).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient<Euangelion>().
                AddIngredient(ItemID.LunarBar, 10).
                AddIngredient<NihilityFragments>(10).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
