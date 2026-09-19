using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    /// <summary>
    /// 先知的图鉴域主题:晨曦符阵。深靛封面配金封边、靛蓝纸面;
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
