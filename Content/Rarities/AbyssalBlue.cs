using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using static CalamityEntropy.Content.Rarities.CERarityNameEffects;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>幽渊紫,深渊档。逐字金到海蓝的渐变字身与同向描边,深海焦散润光缓慢横扫,偶发星芒,字后一条细光线</summary>
    public sealed class AbyssalBlue : CERarity
    {
        public static readonly Color Gold = new(255, 210, 12);
        public static readonly Color SeaBlue = new(140, 180, 255);
        public static readonly Color EdgeLeft = new(70, 110, 255);
        public static readonly Color EdgeRight = new(150, 155, 180);
        public static readonly Color Caustic = new(220, 240, 255);
        /// <summary>拾取主色。解除与虚空紫 (106,40,190) 的撞色</summary>
        public static readonly Color Abyss = new(140, 150, 255);

        //焦散润光高斯半宽 px 与一次横扫周期 s
        private const float SheenSigma = 26f;
        private const float SheenPeriod = 4.6f;
        //星芒槽位数与存活占周期比例
        private const int GlintSlots = 2;
        private const float GlintLife = 0.3f;

        public override Color BaseColor => Abyss;

        //前后缀降档只在自有阶梯内走(2026-09-19 撤掉灾厄 BurnishedAuric/CalamityRed 弱引用)
        public override int GetPrefixedRarity(int offset, float valueMult) => offset switch {
            -2 => ModContent.RarityType<Golden>(),
            -1 => ModContent.RarityType<VoidPurple>(),
            _ => Type,
        };

        public override void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            float fade = FadeOf(lineColor);
            GlyphLayout layout = Layout(text, pos, scale);

            //字后一条压扁的亮线加一层宽柔光
            Color abyss = Fade(Abyss, fade);
            DrawGlowBand(sb, layout, abyss * 0.35f, 40f, 0.35f);
            DrawGlowBand(sb, layout, abyss * 0.18f, 28f, 1.0f);

            //逐字渐变描边,带缓慢的明暗涟漪
            int last = Math.Max(1, layout.Count - 1);
            for (int i = 0; i < layout.Count; i++) {
                float t = i / (float)last;
                float ripple = 1f + 0.2f * MathF.Sin(-time * 4f + i * 0.65f);
                Color edge = Scale(Fade(Color.Lerp(EdgeLeft, EdgeRight, t), fade), ripple);
                for (int d = 0; d < 8; d++) {
                    Vector2 offset = new Vector2(1.2f, 0f).RotatedBy(MathHelper.TwoPi * d / 8f);
                    DrawGlyph(sb, layout, i, offset, edge, scale);
                }
            }

            //金到海蓝的字身,焦散润光按高斯权重扫过
            float span = layout.Width + SheenSigma * 4f;
            float sweepX = layout.Origin.X - SheenSigma * 2f + (time / SheenPeriod % 1f) * span;
            Color caustic = Fade(Caustic, fade);
            for (int i = 0; i < layout.Count; i++) {
                float t = i / (float)last;
                float ripple = 1f + 0.2f * MathF.Sin(-time * 4f + i * 0.65f);
                Color body = Scale(Fade(Color.Lerp(Gold, SeaBlue, t), fade), ripple);
                float d = (layout.CenterX(i) - sweepX) / SheenSigma;
                float w = MathF.Exp(-d * d);
                DrawGlyph(sb, layout, i, Vector2.Zero, Color.Lerp(body, caustic, 0.55f * w), scale);
            }

            //偶发焦散星芒
            for (int k = 0; k < GlintSlots; k++) {
                float period = 2.6f + Hash01(k, 13) * 1.4f;
                float t = Cycle(time, period, Hash01(k, 17), out int index);
                if (t > GlintLife) {
                    continue;
                }
                float intensity = MathF.Sin(MathHelper.Pi * t / GlintLife);
                Vector2 p = new(
                    layout.Origin.X + Hash01(index, k * 5 + 1) * layout.Width,
                    layout.BandY(0.15f + 0.7f * Hash01(index, k * 5 + 2)));
                DrawStar(sb, p, (8f + 4f * intensity) * scale.X, caustic * (0.8f * intensity), intensity * 0.5f);
            }
        }
    }
}
