using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class CESpawnConditionBestiaryInfoElement : FilterProviderInfoElement, IBestiaryBackgroundImagePathAndColorProvider, IBestiaryPrioritizedElement
    {
        private string _backgroundImagePath;
        private Color? _backgroundColor;

        public float OrderPriority { get; set; }

        public CESpawnConditionBestiaryInfoElement(string nameLanguageKey, int filterIconFrame, string backgroundImagePath = null, Color? backgroundColor = null)
            : base(nameLanguageKey, filterIconFrame) {
            _backgroundImagePath = backgroundImagePath;
            _backgroundColor = backgroundColor;
        }

        public Asset<Texture2D> GetBackgroundImage() {
            if (_backgroundImagePath == null)
                return null;

            return ModContent.Request<Texture2D>(_backgroundImagePath);
        }

        public Color? GetBackgroundColor() => _backgroundColor;
    }
}
