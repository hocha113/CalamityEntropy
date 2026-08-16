using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public static class CommonEffects
    {
        public static Effect rotation;
        public static Effect charred;
        public static Effect colorLerp;
        public static Effect LoadShader(string name)
        {
            return ModContent.Request<Effect>($"CalamityEntropy/Assets/Effects/{name}", AssetRequestMode.ImmediateLoad).Value;
        }
        public static void Load()
        {
            rotation = LoadShader("rotation");
            charred = LoadShader("CharredEffect");
            colorLerp = LoadShader("ColorLerp3");
        }
        public static void Unload()
        {
            rotation = null;
            charred = null;
            colorLerp = null;
        }
    }
}
