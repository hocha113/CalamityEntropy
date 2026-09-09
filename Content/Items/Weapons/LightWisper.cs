using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class LightWisper : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 72;
            Item.height = 36;
            Item.damage = 320;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 3;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = SoundID.Item34;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LightWisperFlame>();
            Item.shootSpeed = 11f;
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        public override Vector2? HoldoutOffset() => new Vector2(-28, 0);

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_CleansingBlaze))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_CleansingBlaze)
                .AddIngredient(ModContent.ItemType<VoidBar>(), 5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.ElfMelter)
                .AddIngredient<ChaoticPiece>(10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
