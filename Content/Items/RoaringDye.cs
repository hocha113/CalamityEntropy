using CalamityEntropy.Content.Items.Books.BookMarks;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class RoaringDye : CEBaseDye
    {
        public override ArmorShaderData ShaderDataToBind => new ArmorShaderData(Mod.Assets.Request<Effect>("Assets/Effects/RoaringDye"), "DyePass").
            UseColor(new Color(255, 255, 255)).UseSecondaryColor(new Color(0, 0, 0));
        public override void SafeSetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
        }

        public override void SafeSetDefaults() {
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(0, 0, 50, 0);
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient<BookMarkBlackKnife>().Register();
        }
    }
}
