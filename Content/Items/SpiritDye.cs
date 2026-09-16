using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class SpiritDye : CEBaseDye
    {
        public override ArmorShaderData ShaderDataToBind => new ArmorShaderData(Mod.Assets.Request<Effect>("Assets/Effects/SoulDiscorderDye"), "DyePass").
            UseColor(new Color(200, 200, 255)).UseSecondaryColor(new Color(140, 140, 255)).UseImage(ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/SoulDiscorderColorMap"));
        public override void SafeSetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
        }

        public override void SafeSetDefaults() {
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(0, 2, 50, 0);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<ProphetRelic>()
                .AddTile(TileID.DyeVat)
                .Register();
            CreateRecipe(4)
                .AddIngredient<ProphetTrophy>()
                .AddTile(TileID.DyeVat)
                .Register();
        }
    }
}
