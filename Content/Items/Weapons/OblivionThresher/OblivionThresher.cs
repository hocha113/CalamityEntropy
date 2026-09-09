using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Weapons.Chainsaw;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Cooldowns;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.OblivionThresher
{
    public class OblivionThresher : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 84;
            Item.height = 46;
            Item.damage = 850;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(platinum: 1, gold: 20);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OblivionThresherHoldout>();
            Item.shootSpeed = 24;

        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_SuperradiantSlaughterer, CEID.Item_AscendantSpiritEssence))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_SuperradiantSlaughterer)
                .AddIngredient<VoidBar>(5)
                .AddIngredient(CEID.Item_AscendantSpiritEssence, 2)
                .AddTile<VoidWellTile>()
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Pioneer>()
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>()
                .Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                player.AddCooldown(OblivionThretherCooldown.ID, 320);
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }
        public override bool AltFunctionUse(Player player)
        {
            return !player.HasCooldown(OblivionThretherCooldown.ID);
        }

        public override bool CanShoot(Player player)
        {
            Item.shoot = player.altFunctionUse == 2 ? ModContent.ProjectileType<OblivionCruiserDash>() : ModContent.ProjectileType<OblivionThresherHoldout>();

            return base.CanShoot(player);
        }
    }
}
