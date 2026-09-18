using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>
    /// 辉光系三档(辉绿 / 辉紫 / 天蓝)的共同骨架:暗字身裹亮同色描边,描边随呼吸明暗,字后一层极淡的同色底光。
    /// 子类只给主色,主色同时是拾取飘字色
    /// </summary>
    public abstract class CEGlowRarity : CERarity
    {
        /// <summary>主色</summary>
        protected abstract Color Glow { get; }

        //字身相对主色的压暗比例;描边呼吸周期与明暗区间
        private const float BodyDim = 0.3f;
        private const float BreathPeriod = 2.8f;
        private const float BreathMin = 0.75f;
        private const float BreathMax = 1f;

        public override Color BaseColor => Glow;

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);
            Color main = Fade(Glow, fade);

            DrawGlowBand(sb, layout, main * 0.16f, 20f, 0.85f);
            DrawOutline(sb, text, pos, main * Breath(time, BreathPeriod, BreathMin, BreathMax), scale, 1.2f);
            DrawText(sb, text, pos, Scale(main, BodyDim), scale);
        }
    }
}
