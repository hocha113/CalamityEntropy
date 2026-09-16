using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class GoldenFlowerInAJar : ModItem
    {
        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 38;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
            Item.vanity = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (!hideVisual) {
                if (player.whoAmI == Main.myPlayer) {
                    player.Entropy().SunriseScene = 6;
                }
            }
        }
        public override void UpdateVanity(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.Entropy().SunriseScene = 6;
            }
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Sunflower);
            recipe.AddIngredient(108);
            recipe.AddIngredient(ItemID.Glass, 12);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.Sunflower);
            recipe2.AddIngredient(712);
            recipe2.AddIngredient(ItemID.Glass, 12);
            recipe2.AddTile(TileID.WorkBenches);
            recipe2.Register();
        }

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset) {
            if (line.Text.StartsWith("$") || line.Name == "ItemName") {
                float xj = 0;
                string t = line.Text.StartsWith("$") ? line.Text.Substring(1) : line.Text;
                for (int i = 0; i < t.Length; i++) {
                    float sa = 0.65f + 0.35f * (float)(Math.Sin(i / (t.Length - 1f) * 4f + Main.GlobalTimeWrappedHourly * (line.Name == "ItemName" ? -2 : 6)));
                    string character = t[i].ToString();
                    Vector2 p = new Vector2(line.X + xj, line.Y + (float)(Math.Sin(i / (t.Length - 1f) * 4f + Main.GlobalTimeWrappedHourly * -4) * 1) * (line.Name == "ItemName" ? 1 : -1));
                    for (float r = 0; r < MathHelper.TwoPi; r += MathHelper.PiOver4) {
                        Main.spriteBatch.DrawString(FontAssets.MouseText.Value, character, p + r.ToRotationVector2() * 2, (line.Name == "ItemName" ? Color.Gold * 0.6f : Color.Pink) * sa);
                    }
                    Main.spriteBatch.DrawString(FontAssets.MouseText.Value, character, p, line.Name == "ItemName" ? Color.Pink * 2 : Color.Gold * 1.1f);
                    xj += FontAssets.MouseText.Value.MeasureString(character).X;
                }
                return false;
            }
            return true;
        }
    }
}
