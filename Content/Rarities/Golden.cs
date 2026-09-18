using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>鎏金,巡游者后终局档。正金字身裹深金褐描边,镜面高光逐字掠过,数秒一次整体亮闪,暖色底光</summary>
    public sealed class Golden : CERarity
    {
        /// <summary>拾取主色,也是字身色</summary>
        public static readonly Color Gold = new(246, 200, 0);
        public static readonly Color GoldEdge = new(120, 78, 10);
        public static readonly Color Specular = new(255, 250, 225);
        public static readonly Color Warm = new(210, 180, 120);

        //整体亮闪的周期 s 与占比
        private const float FlashPeriod = 6.5f;
        private const float FlashWidth = 0.05f;
        //镜面高光的流速与锐度:pow(sin, N) 只在极窄区间可见
        private const float SpecularSpeed = 1.5f;
        private const float SpecularPower = 90f;

        public override Color BaseColor => Gold;

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);

            DrawGlowBand(sb, layout, Fade(Warm, fade) * 0.35f, 32f, 0.9f);
            DrawOutline(sb, text, pos, Fade(GoldEdge, fade), scale, 1.6f);

            //正金字身,数秒一次整体亮闪
            Color specular = Fade(Specular, fade);
            float flashT = (time / FlashPeriod + 0.37f) % 1f;
            float flash = flashT < FlashWidth ? MathF.Sin(MathHelper.Pi * flashT / FlashWidth) : 0f;
            DrawText(sb, text, pos, Color.Lerp(Fade(Gold, fade), specular, flash * 0.85f), scale);

            //镜面高光逐字掠过,其余字符直接跳过
            Color shine = Additive(specular);
            for (int i = 0; i < layout.Count; i++) {
                float s = (MathF.Sin(layout.CenterX(i) * 0.02f - time * SpecularSpeed) + 1f) * 0.5f;
                float strength = MathF.Pow(s, SpecularPower);
                if (strength < 1f / 255f) {
                    continue;
                }
                DrawGlyph(sb, layout, i, Vector2.Zero, shine * (strength * 0.9f), scale);
            }
        }
    }
}
