using CalamityEntropy.Content.Tiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class GiantBoulder : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults() {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.autoReuse = false;
            Item.consumable = true;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 2);
            Item.rare = ItemRarityID.Yellow;
            Item.DefaultToPlaceableTile(ModContent.TileType<GiantBoulderTile>(), 0);
            Item.width = 160;
            Item.height = 160;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.Boulder, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            CEUtils.DrawInventoryCustomScale(
                spriteBatch,
                texture: TextureAssets.Item[Type].Value,
                position,
                frame,
                drawColor,
                itemColor,
                Vector2.One * 42,
                scale,
                wantedScale: 0.5f,
                drawOffset: new(-1f, 0f)
            );
            return false;
        }
    }
}
