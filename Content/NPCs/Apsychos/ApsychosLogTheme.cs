using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    /// <summary>
    /// 无魂者的图鉴域主题:灰烬余火。炭黑封面配余烬橙封边、焦褐纸面;
    /// 书后是灰烬荒原的暮空(地平线一带余火背光、余烬上浮、热浪线、飘灰),
    /// 书脊上一条火线噼啪跳动,底封边余烬顺风滑行
    /// </summary>
    internal sealed class ApsychosLogTheme : CEBossLogTheme
    {
        public static readonly ApsychosLogTheme Instance = new();

        /// <summary>一阶段焰红系(与战斗端描边 (255,80,40) 同调)/ 二阶段幽蓝系(与 (70,70,255) 同调)</summary>
        public static readonly Color EmberOrange = new(255, 110, 45);
        public static readonly Color EmberHot = new(255, 200, 150);
        public static readonly Color SoulBlue = new(80, 90, 255);
        public static readonly Color SoulPale = new(170, 180, 255);
        public static readonly Color Ash = new(120, 100, 96);
        /// <summary>沙盒天幕两阶段的两端色</summary>
        public static readonly Color AshSkyTop = new(26, 10, 8);
        public static readonly Color AshSkyBottom = new(92, 36, 22);
        public static readonly Color SoulSkyTop = new(8, 10, 34);
        public static readonly Color SoulSkyBottom = new(28, 38, 96);

        public override Color Cover => new(34, 18, 14);
        public override Color CoverEdge => new(220, 120, 60);
        public override Color Spine => new(22, 10, 8);
        public override Color Paper => new(52, 30, 26);
        public override Color PaperLight => new(220, 150, 100);
        public override Color Rule => new(160, 90, 60);
        public override Color Accent => new(255, 150, 70);
        public override Color Muted => new(210, 170, 140);
        public override Color Dim => new(10, 4, 3);
        public override Color SkyTop => AshSkyTop;
        public override Color SkyBottom => new(110, 40, 24);

        public override void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //地平余火:屏幕下沿一大团暗火背光,呼吸
            float breath = 0.85f + 0.15f * MathF.Sin(time * 1.7f);
            CEPortraitDraw.Glow(sb, new Vector2(screen.Center.X, screen.Bottom + 60f), new Vector2(screen.Width * 1.4f, 520f), EmberOrange * (0.3f * breath * blend));
            CEPortraitDraw.Glow(sb, new Vector2(book.X - 160f, book.Y - 40f), 520f, new Color(120, 40, 20) * (0.3f * blend));

            //余烬:自下而上飘升的亮粒,左右轻摆、越高越淡、闪烁
            for (int i = 0; i < 80; i++) {
                float hx = CEPortraitDraw.Hash01(i, 2.9f);
                float hy = CEPortraitDraw.Hash01(i, 7.7f);
                float rise = (time * (26f + hx * 30f) + hy * 1500f) % (screen.Height + 80f);
                float y = screen.Bottom + 40f - rise;
                float x = screen.X + hx * screen.Width + MathF.Sin(time * 0.9f + i * 1.7f) * 22f;
                if (book.Contains((int)x, (int)y)) {
                    continue;
                }
                float fade = MathHelper.Clamp(rise / screen.Height, 0f, 1f);
                float flick = 0.6f + 0.4f * MathF.Sin(time * (6f + hx * 8f) + i);
                float a = (0.7f - 0.5f * fade) * flick * blend;
                Color c = Color.Lerp(EmberOrange, EmberHot, hy * 0.7f);
                float s = 1.5f + hx * 1.5f;
                CEPortraitDraw.Fill(sb, new Vector2(x, y), new Vector2(s, s), c with { A = 0 } * a);
                if (i % 5 == 0) {
                    CEPortraitDraw.Glow(sb, new Vector2(x, y), 14f + hx * 10f, c * (a * 0.6f));
                }
            }

            //热浪线:横向细亮线自下缓升,越高越淡
            for (int i = 0; i < 14; i++) {
                float ph = i * 71.3f;
                float rise = (time * (24f + i % 4 * 8f) + ph) % (screen.Height + 40f);
                float y = screen.Bottom - rise;
                float fade = 1f - rise / (screen.Height + 40f);
                float x = screen.X + CEPortraitDraw.Hash01(i, 3.3f) * screen.Width + MathF.Sin(time * 0.8f + ph) * 40f;
                float len = 50f + i % 3 * 30f;
                CEPortraitDraw.Line(sb, new Vector2(x - len, y), new Vector2(x + len, y), 1.3f, EmberHot with { A = 0 } * (0.12f * fade * blend));
            }

            //飘灰:屏幕下沿几团暗灰烟羽缓移
            for (int i = 0; i < 4; i++) {
                float ph = i * 1.9f;
                float x = (time * (9f + i * 3f) + ph * 240f) % (screen.Width + 600f) - 300f;
                float y = screen.Bottom - 80f - 40f * MathF.Sin(ph);
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 440f + i * 60f, Ash * (0.12f * blend), ph + time * 0.03f);
            }
        }

        public override void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottom, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //书脊火线:沿脊心的一条噼啪跳动的亮线(按 10 帧一换的时间桶取随机亮段),两端渐暗
            if (spine.Height > 30) {
                int bucket = (int)(time * 10f);
                float cx = spine.Center.X;
                int steps = Math.Max(6, spine.Height / 12);
                float step = (spine.Height - 16f) / steps;
                for (int i = 0; i < steps; i++) {
                    float y = spine.Y + 8f + i * step;
                    float edge = MathF.Sin(i / (float)(steps - 1) * MathHelper.Pi);
                    float h = CEPortraitDraw.Hash01(bucket * 37 + i, 6.1f);
                    float hot = h > 0.6f ? (h - 0.6f) / 0.4f : 0f;
                    Color c = Color.Lerp(EmberOrange, EmberHot, hot) with { A = 0 };
                    float w = 1.5f + hot * 2.5f;
                    CEPortraitDraw.Fill(sb, new Vector2(cx - w * 0.5f, y), new Vector2(w, step + 0.5f), c * ((0.25f + 0.55f * hot) * edge * blend));
                }
                //脊心余火柔光
                CEPortraitDraw.Glow(sb, spine.Center.ToVector2(), new Vector2(spine.Width * 1.8f, spine.Height * 0.7f), EmberOrange * (0.25f * blend));
            }
            //底封边:余烬顺风滑行,忽明忽暗
            if (bottom.Height >= 3) {
                int h = Math.Max(1, bottom.Height - 2);
                for (int i = 0; i < 16; i++) {
                    float ph = i * 43.7f;
                    float x = bottom.X + (time * (30f + i % 4 * 10f) + ph) % bottom.Width;
                    float y = bottom.Y + 1 + ph * 0.31f % h;
                    float flick = 0.5f + 0.5f * MathF.Sin(time * (5f + i % 3) + ph);
                    Color c = Color.Lerp(EmberOrange, EmberHot, flick) with { A = 0 };
                    CEPortraitDraw.Fill(sb, new Vector2(x, y), new Vector2(2f, 1f), c * ((0.3f + 0.5f * flick) * blend));
                }
            }
        }
    }
}
