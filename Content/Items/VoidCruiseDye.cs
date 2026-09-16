using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class VoidCruiseDye : CEBaseDye
    {
        public override ArmorShaderData ShaderDataToBind => new ArmorShaderData(Mod.Assets.Request<Effect>("Assets/Effects/VoidCruiserDye"), "DyePass").
            UseColor(new Color(160, 150, 255)).UseSecondaryColor(new Color(210, 200, 255));
        public override void SafeSetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
        }

        public override void SafeSetDefaults() {
            Item.rare = ModContent.RarityType<Golden>();
            Item.value = Item.sellPrice(0, 4, 0, 0);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<VoidScales>(5)
                .AddTile(TileID.DyeVat)
                .Register();
        }
    }
}
