using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Menu
{
    public class MenuBack : ModSurfaceBackgroundStyle
    {
        public override void ModifyFarFades(float[] fades, float transitionSpeed) {
            for (int i = 0; i < fades.Length; i++) {
                if (i == Slot) {
                    fades[i] += transitionSpeed;
                    if (fades[i] > 1f) {
                        fades[i] = 1f;
                    }
                }
                else {
                    fades[i] -= transitionSpeed;
                    if (fades[i] < 0f) {
                        fades[i] = 0f;
                    }
                }
            }
        }

        private static readonly string TexPath = "CalamityEntropy/Assets/Extra/Ports/Backgrounds/BlankPixel";
        public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b) => BackgroundTextureLoader.GetBackgroundSlot(TexPath);
        public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(TexPath);
        public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(TexPath);
        public override bool PreDrawCloseBackground(SpriteBatch spriteBatch) => false;
    }
}