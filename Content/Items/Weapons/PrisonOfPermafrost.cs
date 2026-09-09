using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class PrisonOfPermafrost : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.DamageType = DamageClass.Magic;
            Item.width = 96;
            Item.noUseGraphic = true;
            Item.height = 96;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.channel = true;
            Item.knockBack = 5;
            Item.value = 145000;
            Item.rare = CECal.RarityHotPink(ModContent.RarityType<VoidPurple>());
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<PrisonOfPermafrostCircle>();
            Item.shootSpeed = 1f;
            Item.mana = 20;
            Item.useStyle = -1;
            Item.noMelee = true;
            Item.crit = 5;
            Item.Entropy().tooltipStyle = 1;
            Item.Entropy().stroke = true;
            Item.Entropy().strokeColor = new Color(70, 210, 250);
            Item.Entropy().NameColor = new Color(200, 0, 200);
            Item.Entropy().HasCustomStrokeColor = true;
            Item.Entropy().HasCustomNameColor = true;
        }
        public override bool MagicPrefix()
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

            player.channel = true;
            if (player.ownedProjectileCounts[type] < 1)
            {
                return true;
            }
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_IceBarrage, CEID.Item_GlacialEmbrace, CEID.Item_AuricBar, CEID.Item_AscendantSpiritEssence, CEID.Tile_CosmicAnvil))
            {
                Recipe calRecipe = CreateRecipe();
                calRecipe.AddIngredient(CEID.Item_IceBarrage, 1);
                calRecipe.AddIngredient(CEID.Item_GlacialEmbrace, 1);
                calRecipe.AddIngredient(CEID.Item_AuricBar, 5);
                calRecipe.AddIngredient(CEID.Item_AscendantSpiritEssence, 2);
                calRecipe.AddTile(CEID.Tile_CosmicAnvil);
                calRecipe.Register();
                return;
            }
            Recipe ownRecipe = CreateRecipe();
            ownRecipe.AddIngredient(ItemID.BlizzardStaff);
            ownRecipe.AddIngredient(ItemID.FrostStaff);
            ownRecipe.AddIngredient(ItemID.LunarBar, 5);
            ownRecipe.AddTile(TileID.LunarCraftingStation);
            ownRecipe.Register();
        }
    }
}
