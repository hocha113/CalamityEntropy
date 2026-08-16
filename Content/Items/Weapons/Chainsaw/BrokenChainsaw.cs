using CalamityEntropy.Content.Projectiles.Chainsaw;
using CalamityMod;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Chainsaw
{
    public class BrokenChainsaw : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 5;
            Item.DamageType = ModContent.GetInstance<TrueMeleeDamageClass>();
            Item.width = 42;
            Item.height = 42;
            Item.noUseGraphic = true;
            Item.useTime = Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = 36;
            Item.rare = ItemRarityID.Gray;
            Item.UseSound = SoundID.Item23;
            Item.channel = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<BrokenChainsaw0>();
            Item.shootSpeed = 1f;
        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<DubiousPlating>(5).
                AddIngredient(ItemID.IronBar, 10).
                AddIngredient(ItemID.Chain, 1).
                AddTile(TileID.Anvils).
                Register();
            CreateRecipe().
                AddIngredient<DubiousPlating>(5).
                AddIngredient(ItemID.LeadBar, 10).
                AddIngredient(ItemID.Chain, 1).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}