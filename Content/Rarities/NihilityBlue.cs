using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>虚无蓝,虚无双子档。深靛字身裹紫罗兰呼吸描边,一道高光带横扫,字后细光丝横向流过(保留旧签名)</summary>
    public sealed class NihilityBlue : CERarity
    {
        public static readonly Color Indigo = new(20, 12, 60);
        public static readonly Color Violet = new(180, 70, 255);
        public static readonly Color Deep = new(60, 5, 255);
        public static readonly Color Pale = new(245, 200, 255);
        /// <summary>拾取主色</summary>
        public static readonly Color Nihil = new(120, 60, 255);

        //字后光丝根数;每秒横向流过行宽的比例
        private const int StreakCount = 12;
        private const float StreakSpeed = 0.18f;
        //高光带高斯半宽 px 与一次横扫周期 s
        private const float SheenSigma = 22f;
        private const float SheenPeriod = 3.6f;

        public override Color BaseColor => Nihil;

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);

            DrawGlowBand(sb, layout, Fade(Violet, fade) * 0.12f, 20f, 0.8f);

            //字后细光丝:各自哈希高度、长度、相位,两端淡入淡出,横向略超出行宽
            Color deep = Fade(Deep, fade);
            Color pale = Fade(Pale, fade);
            for (int k = 0; k < StreakCount; k++) {
                float u = Hash01(k, 1) - time * StreakSpeed * (0.8f + 0.4f * Hash01(k, 2));
                u -= MathF.Floor(u);
                float alpha = u < 0.2f ? u / 0.2f : u > 0.8f ? (1f - u) / 0.2f : 1f;
                float x = layout.Origin.X + (0.5f + (u - 0.5f) * 1.3f) * layout.Width;
                float y = layout.BandY(Hash01(k, 3));
                float length = 30f + 30f * Hash01(k, 4);
                float thickness = (1.2f + Hash01(k, 5)) * scale.Y;
                Color c = Color.Lerp(deep, pale, 4f * u * (1f - u));
                DrawStreak(sb, new Vector2(x, y), length * scale.X, thickness, c * (0.45f * alpha));
            }

            DrawOutline(sb, text, pos, Fade(Violet, fade) * Breath(time, 2.6f, 0.55f, 0.85f), scale, 1.4f);

            //深靛字身,高光带按高斯权重扫过
            Color indigo = Fade(Indigo, fade);
            float span = layout.Width + SheenSigma * 4f;
            float sweepX = layout.Origin.X - SheenSigma * 2f + (time / SheenPeriod % 1f) * span;
            for (int i = 0; i < layout.Count; i++) {
                float d = (layout.CenterX(i) - sweepX) / SheenSigma;
                float w = MathF.Exp(-d * d);
                DrawGlyph(sb, layout, i, Vector2.Zero, Color.Lerp(indigo, pale, 0.7f * w), scale);
            }
        }
    }
}
