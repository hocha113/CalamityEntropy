using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class GhostdomWhisper : ModItem, IDevItem
    {
        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 80;
            Item.damage = 1500;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 5;
            Item.useAnimation = 5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 8f;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.shootSpeed = 22f;
            Item.useAmmo = AmmoID.Arrow;
            Item.channel = true;
            Item.noUseGraphic = true;
        }
        public bool cs = false;

        public string DevName => "Polaris";

        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return cs;
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        public override Vector2? HoldoutOffset() => new Vector2(-28, 0);
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AstrealDefeat, CEID.Item_DarkPlasma))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_AstrealDefeat, 1)
                .AddIngredient(CEID.Item_DarkPlasma, 8)
                .AddIngredient(ModContent.ItemType<VoidBar>(), 5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Kinanition>()
                .AddIngredient<VoidBar>(5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
