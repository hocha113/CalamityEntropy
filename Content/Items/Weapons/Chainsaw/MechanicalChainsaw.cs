using CalamityEntropy.Content.Projectiles.Chainsaw;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Chainsaw
{
    public class MechanicalChainsaw : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Melee;
            Item.width = 42;
            Item.height = 42;
            Item.noUseGraphic = true;
            Item.useTime = Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.ArmorPenetration = 45;
            Item.value = 2000000;
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item23;
            Item.channel = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<MechanicalChainsaw0>();
            Item.shootSpeed = 1f;
        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<BrokenChainsaw>().
                AddIngredient(ItemID.AdamantiteBar, 10).
                AddCalOrOwn(CEID.Item_EssenceofHavoc, ItemID.SoulofNight, 3).
                AddTile(TileID.MythrilAnvil).
                Register();
            CreateRecipe().
                AddIngredient<BrokenChainsaw>().
                AddIngredient(ItemID.TitaniumBar, 10).
                AddCalOrOwn(CEID.Item_EssenceofHavoc, ItemID.SoulofNight, 3).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}