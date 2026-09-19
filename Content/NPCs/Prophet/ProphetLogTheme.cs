using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    /// <summary>
    /// 先知的图鉴域主题:晨曦符阵。深靛封面配金封边、靛蓝纸面;
    /// 书后是晨光天穹(右上晨日斜射光柱、低处云霭、绕书一大一小两圈反向慢转的符文环),
    /// 书脊上一枚符轮缓缓上下巡游,底封边金色星芒错落闪烁
    /// </summary>
    internal sealed class ProphetLogTheme : CEBossLogTheme
    {
        public static readonly ProphetLogTheme Instance = new();

        /// <summary>符文蓝 / 圣白 / 晨金(与战斗端 SkyBlue / White 光效同调)</summary>
        public static readonly Color RuneBlue = new(120, 200, 255);
        public static readonly Color HolyWhite = new(240, 248, 255);
        public static readonly Color DawnGold = new(255, 226, 170);
        /// <summary>沙盒天穹的两端色</summary>
        public static readonly Color SkyDeep = new(18, 30, 78);
        public static readonly Color SkyDawn = new(112, 158, 216);

        public override Color Cover => new(26, 42, 96);
        public override Color CoverEdge => new(222, 196, 132);
        public override Color Spine => new(16, 28, 68);
        public override Color Paper => new(38, 54, 100);
        public override Color PaperLight => new(188, 208, 244);
        public override Color Rule => new(118, 150, 212);
        public override Color Accent => new(226, 240, 255);
        public override Color Muted => new(176, 196, 232);
        public override Color Dim => new(5, 8, 22);
        public override Color SkyTop => SkyDeep;
        public override Color SkyBottom => new(84, 128, 196);

        public override void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //晨日:书右上一轮暖白盘与大范围金晕
            Vector2 sun = new(book.Right + 150f, book.Y - 110f);
            CEPortraitDraw.Glow(sb, sun, 640f, DawnGold * (0.4f * blend));
            CEPortraitDraw.Glow(sb, sun, 180f, HolyWhite * (0.55f * blend));

            //斜射光柱:自晨日向左下扇开,缓慢摆动、明暗交替
            for (int i = 0; i < 8; i++) {
                float ang = MathHelper.ToRadians(108f + i * 9f + MathF.Sin(time * 0.3f + i) * 2f);
                Vector2 dir = ang.ToRotationVector2();
                float a = (0.05f + 0.04f * MathF.Sin(time * 0.7f + i * 1.7f)) * blend;
                float w = 14f + i % 3 * 10f;
                CEPortraitDraw.Line(sb, sun + dir * 80f, sun + dir * 2400f, w, HolyWhite with { A = 0 } * a);
            }

            //云霭:屏幕下沿几团白雾缓移
            for (int i = 0; i < 5; i++) {
                float ph = i * 1.6f;
                float x = (time * (10f + i * 3f) + ph * 260f) % (screen.Width + 700f) - 350f;
                float y = screen.Bottom - 70f - 50f * MathF.Sin(ph);
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 520f + i * 60f, HolyWhite * (0.09f * blend), ph + time * 0.02f);
            }

            //绕书符文环:一大一小两圈反向慢转,大环带 12 个刻点
            Vector2 center = book.Center.ToVector2();
            float rx = book.Width * 0.62f;
            float ry = book.Height * 0.72f;
            CEPortraitDraw.Ellipse(sb, center, new Vector2(rx, ry), 0f, RuneBlue with { A = 0 } * (0.16f * blend), 1.6f, 72,
                time * 0.5f, HolyWhite with { A = 0 } * (0.45f * blend));
            CEPortraitDraw.Ellipse(sb, center, new Vector2(rx + 26f, ry + 26f), 0f, RuneBlue with { A = 0 } * (0.1f * blend), 1f, 72,
                -time * 0.35f, RuneBlue with { A = 0 } * (0.4f * blend));
            for (int i = 0; i < 12; i++) {
                float ang = MathHelper.TwoPi * i / 12f + time * 0.5f;
                Vector2 p = center + new Vector2(MathF.Cos(ang) * rx, MathF.Sin(ang) * ry);
                if (book.Contains((int)p.X, (int)p.Y)) {
                    continue;
                }
                float tw = 0.5f + 0.5f * MathF.Sin(time * 2f + i);
                CEPortraitDraw.Star(sb, p, 3.5f + 1.5f * tw, HolyWhite with { A = 0 } * ((0.3f + 0.4f * tw) * blend), 1.2f);
            }

            //浮符星点:确定性散布,慢闪
            for (int i = 0; i < 60; i++) {
                float hx = CEPortraitDraw.Hash01(i, 2.8f);
                float hy = CEPortraitDraw.Hash01(i, 6.3f);
                Vector2 p = new(screen.X + hx * screen.Width, screen.Y + hy * screen.Height * 0.85f);
                if (book.Contains((int)p.X, (int)p.Y)) {
                    continue;
                }
                float tw = 0.5f + 0.5f * MathF.Sin(time * (0.7f + hx * 1.3f) + i * 2.3f);
                float a = (0.18f + 0.42f * tw) * blend;
                if (i % 6 == 0) {
                    CEPortraitDraw.Star(sb, p, 3f + 2f * tw, DawnGold with { A = 0 } * a);
                }
                else {
                    CEPortraitDraw.Fill(sb, p, new Vector2(1.5f, 1.5f), HolyWhite with { A = 0 } * a);
                }
            }
        }

        public override void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottom, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //书脊符轮:一枚窄椭圆符环沿脊心缓缓上下巡游,带金色亮弧,环心一点柔光
            if (spine.Height > 40) {
                float y = spine.Y + 24f + (spine.Height - 48f) * (0.5f + 0.5f * MathF.Sin(time * 0.45f));
                Vector2 c = new(spine.Center.X, y);
                CEPortraitDraw.Glow(sb, c, new Vector2(spine.Width * 1.8f, 90f), RuneBlue * (0.35f * blend));
                CEPortraitDraw.Ellipse(sb, c, new Vector2(spine.Width * 0.42f, 22f), 0f, RuneBlue with { A = 0 } * (0.55f * blend), 1.2f, 28,
                    time * 2f, DawnGold with { A = 0 } * (0.8f * blend));
                CEPortraitDraw.Ellipse(sb, c, new Vector2(spine.Width * 0.28f, 12f), 0f, HolyWhite with { A = 0 } * (0.4f * blend), 1f, 20,
                    -time * 2.6f, HolyWhite with { A = 0 } * (0.6f * blend));
            }
            //底封边:金色星芒错落闪烁
            if (bottom.Height >= 3) {
                int bucket = (int)(time * 3f);
                for (int i = 0; i < 16; i++) {
                    float h = CEPortraitDraw.Hash01(bucket * 13 + i, 4.1f);
                    if (h < 0.45f) {
                        continue;
                    }
                    float x = bottom.X + CEPortraitDraw.Hash01(i, 9.2f) * bottom.Width;
                    float y = bottom.Center.Y;
                    float a = (h - 0.45f) / 0.55f * (0.5f + 0.5f * MathF.Sin(time * 6f + i)) * blend;
                    CEPortraitDraw.Star(sb, new Vector2(x, y), MathF.Min(3f, bottom.Height * 0.45f), DawnGold with { A = 0 } * (0.9f * a));
                }
            }
        }
    }
}
