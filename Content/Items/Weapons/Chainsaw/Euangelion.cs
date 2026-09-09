using CalamityEntropy.Content.Projectiles.Chainsaw;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Chainsaw
{
    public class Euangelion : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 230;
            Item.DamageType = DamageClass.Melee;
            Item.width = 42;
            Item.height = 42;
            Item.noUseGraphic = true;
            Item.useTime = 16;
            Item.useAnimation = 0;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = 36;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item23;
            Item.channel = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Euangelion0>();
            Item.shootSpeed = 1f;
        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_UnholyEssence, CEID.Item_Necroplasm))
            {
                CreateRecipe().
                AddIngredient<EnslavedStar>().
                AddIngredient(ItemID.LunarBar, 5).
                AddIngredient(CEID.Item_UnholyEssence, 10).
                AddIngredient(CEID.Item_Necroplasm, 5).
                AddTile(TileID.LunarCraftingStation).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient<EnslavedStar>().
                AddIngredient(ItemID.FragmentSolar, 4).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
