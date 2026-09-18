using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>月痕蓝,幻光星蛾档。金、藕紫、青三段渐变字身带缓慢明暗涟漪,蛾翼鳞粉两色交替自下而上漂浮</summary>
    public sealed class Lunarblight : CERarity
    {
        public static readonly Color Mauve = new(160, 106, 150);
        public static readonly Color MoonGold = new(255, 255, 120);
        public static readonly Color MoonCyan = new(90, 255, 225);
        public static readonly Color Dusk = new(40, 22, 60);
        /// <summary>拾取主色。与虚无蓝 (120,60,255) 拉开,偏薰衣草</summary>
        public static readonly Color Lavender = new(150, 130, 220);

        //鳞粉槽位数;存活占周期比例;上浮速度 px/s
        private const int DustSlots = 4;
        private const float DustLife = 0.7f;
        private const float DustRise = 22f;
        //明暗涟漪:沿 X 的空间频率与流速
        private const float RippleFreq = 0.05f;
        private const float RippleSpeed = 3f;

        public override Color BaseColor => Lavender;

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);

            DrawGlowBand(sb, layout, Fade(Lavender, fade) * Breath(time, 3.2f, 0.10f, 0.18f), 24f, 0.9f);
            DrawOutline(sb, text, pos, Fade(Dusk, fade), scale, 1.2f);

            //三段渐变:左端金、中段藕紫、右端青;一道缓慢的明暗涟漪沿字流过
            Color gold = Fade(MoonGold, fade);
            Color mauve = Fade(Mauve, fade);
            Color cyan = Fade(MoonCyan, fade);
            for (int i = 0; i < layout.Count; i++) {
                float cx = layout.CenterX(i);
                float p = layout.Width > 0f ? (cx - layout.Origin.X) / layout.Width : 0.5f;
                Color c = p < 0.5f ? Color.Lerp(gold, mauve, p * 2f) : Color.Lerp(mauve, cyan, (p - 0.5f) * 2f);
                float ripple = 1f + 0.15f * MathF.Sin(cx * RippleFreq - time * RippleSpeed);
                DrawGlyph(sb, layout, i, Vector2.Zero, Scale(c, ripple), scale);
            }

            //蛾翼鳞粉:金青两色交替,自字底缓慢上漂并左右轻摆,淡入淡出
            for (int k = 0; k < DustSlots; k++) {
                float period = 1.6f + Hash01(k, 7) * 1.0f;
                float t = Cycle(time, period, Hash01(k, 3), out int index);
                if (t > DustLife) {
                    continue;
                }
                float age = t * period;
                float alive = MathF.Sin(MathHelper.Pi * t / DustLife);
                Vector2 p = new(
                    layout.Origin.X + Hash01(index, k * 3 + 1) * layout.Width + MathF.Sin(age * 4f + index) * 3f,
                    layout.BandY(1f) - DustRise * age);
                Color c = (k & 1) == 0 ? gold : cyan;
                DrawMote(sb, p, (7f + 3f * alive) * scale.X, c * (alive * 0.9f));
                DrawFleck(sb, p, 1.2f * scale.X, Color.White * (fade * alive * 0.8f));
            }
        }
    }
}
