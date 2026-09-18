using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>
    /// 紫罗兰闪光,特殊档。水晶字:脉动旋转的多向淡影、竖向光条、逐字渐变亮边裹黑芯、上浮闪粒,
    /// 本体在 <see cref="CERarityNameEffects.DrawCrystal(SpriteBatch, Item, string, Vector2, Vector2, float, Color, Color, Color, Color, bool)"/>,
    /// 个别物品(FlowingLight、FadingRoseateReverie)换色复用同一原语
    /// </summary>
    public sealed class ShiningViolet : CERarity
    {
        /// <summary>拾取主色</summary>
        public static readonly Color Violet = Color.Violet;
        //亮边起始色偏暗,渐变到右端的正紫罗兰
        public static readonly Color EdgeStart = Color.Violet * 0.6f;
        public static readonly Color Light = Color.Purple;

        public override Color BaseColor => Violet;

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            DrawCrystal(sb, item, text, pos, scale, time,
                Fade(EdgeStart, fade), Fade(Violet, fade), Fade(Light, fade), Fade(Violet, fade), true);
        }
    }
}
