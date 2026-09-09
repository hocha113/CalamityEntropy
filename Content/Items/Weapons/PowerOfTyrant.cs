using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class PowerOfTyrant : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.crit = 16;
            Item.DamageType = DamageClass.Melee;
            Item.width = 142;
            Item.noUseGraphic = true;
            Item.height = 142;
            Item.useTime = 4;
            Item.useAnimation = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = null;
            Item.channel = true;
            Item.ArmorPenetration = 40;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<PoTProj>();
            Item.shootSpeed = 6f;
        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<PoTProj>()] < 1;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DefiledGreatsword, CEID.Item_NightmareFuel))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_DefiledGreatsword, 1).
                AddIngredient(CEID.Item_NightmareFuel, 10).
                AddIngredient(ModContent.ItemType<VoidBar>(), 5).
                AddTile(ModContent.TileType<VoidWellTile>()).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient<RuneSong>().
                AddIngredient<ChaoticPiece>(5).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
}
