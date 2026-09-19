using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth
{
    /// <summary>
    /// 月华之蛾的图鉴域主题:月夜银翼。午夜靛蓝封面配银封边、靛蓝纸面;
    /// 书后是满月夜空(右上一轮满月与大范围月晕、星野慢闪、月尘上浮、低处夜霭),
    /// 书脊上银色星芒错落闪烁,底封边一道柔光缓缓来回
    /// </summary>
    internal sealed class LuminarisLogTheme : CEBossLogTheme
    {
        public static readonly LuminarisLogTheme Instance = new();

        /// <summary>两阶段附魔色(与战斗端 DrawMyself 的 (0,190,250) / (160,80,255) 同值)</summary>
        public static readonly Color PhaseCyan = new(0, 190, 250);
        public static readonly Color PhaseViolet = new(160, 80, 255);
        public static readonly Color MoonWhite = new(236, 242, 255);
        /// <summary>沙盒夜空的两端色</summary>
        public static readonly Color NightTop = new(5, 8, 26);
        public static readonly Color NightBottom = new(24, 32, 74);

        public override Color Cover => new(10, 16, 44);
        public override Color CoverEdge => new(190, 200, 230);
        public override Color Spine => new(6, 10, 30);
        public override Color Paper => new(26, 32, 64);
        public override Color PaperLight => new(160, 180, 230);
        public override Color Rule => new(100, 120, 190);
        public override Color Accent => new(200, 235, 255);
        public override Color Muted => new(160, 175, 215);
        public override Color Dim => new(3, 4, 12);
        public override Color SkyTop => NightTop;
        public override Color SkyBottom => new(30, 40, 90);

        public override void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //满月:书右上一轮实心盘 + 月晕
            Vector2 moon = new(book.Right + 130f, book.Y - 90f);
            const float MoonR = 54f;
            CEPortraitDraw.Glow(sb, moon, 720f, new Color(150, 190, 255) * (0.3f * blend));
            CEPortraitDraw.Glow(sb, moon, 220f, MoonWhite * (0.45f * blend));
            for (float y = -MoonR; y <= MoonR; y += 2f) {
                float k = y / MoonR;
                float hw = MoonR * MathF.Sqrt(MathF.Max(0f, 1f - k * k));
                if (hw < 0.5f) {
                    continue;
                }
                Color c = Color.Lerp(MoonWhite, new Color(190, 205, 240), MathF.Abs(k) * 0.6f) * blend;
                CEPortraitDraw.Fill(sb, new Vector2(moon.X - hw, moon.Y + y), new Vector2(hw * 2f, 2.2f), c);
            }

            //星野:确定性散布,慢闪
            for (int i = 0; i < 120; i++) {
                float hx = CEPortraitDraw.Hash01(i, 1.7f);
                float hy = CEPortraitDraw.Hash01(i, 6.9f);
                Vector2 p = new(screen.X + hx * screen.Width, screen.Y + hy * screen.Height);
                if (book.Contains((int)p.X, (int)p.Y) || Vector2.Distance(p, moon) < MoonR + 8f) {
                    continue;
                }
                float tw = 0.5f + 0.5f * MathF.Sin(time * (0.7f + hx * 1.5f) + i * 1.9f);
                float a = (0.2f + 0.5f * tw) * blend;
                if (i % 9 == 0) {
                    CEPortraitDraw.Star(sb, p, 3f + 2f * tw, MoonWhite with { A = 0 } * (a * 0.8f));
                }
                else {
                    float s = 1f + hy * 1.2f;
                    CEPortraitDraw.Fill(sb, p, new Vector2(s, s), MoonWhite with { A = 0 } * a);
                }
            }

            //月尘:柔光小点上浮、左右轻摆,越高越淡
            for (int i = 0; i < 50; i++) {
                float hx = CEPortraitDraw.Hash01(i, 3.4f);
                float hy = CEPortraitDraw.Hash01(i, 8.8f);
                float rise = (time * (12f + hx * 16f) + hy * 1200f) % (screen.Height + 60f);
                float y = screen.Bottom + 30f - rise;
                float x = screen.X + hx * screen.Width + MathF.Sin(time * 0.5f + i * 1.1f) * 16f;
                if (book.Contains((int)x, (int)y)) {
                    continue;
                }
                float fade = MathHelper.Clamp(rise / screen.Height, 0f, 1f);
                float a = (0.45f - 0.3f * fade) * blend;
                Color c = Color.Lerp(PhaseCyan, MoonWhite, 0.5f + 0.5f * hy);
                CEPortraitDraw.Glow(sb, new Vector2(x, y), 8f + hx * 10f, c * a);
            }

            //夜霭:屏幕下沿几团靛蓝薄雾缓移
            for (int i = 0; i < 4; i++) {
                float ph = i * 1.8f;
                float x = (time * (8f + i * 3f) + ph * 280f) % (screen.Width + 600f) - 300f;
                float y = screen.Bottom - 60f - 40f * MathF.Sin(ph);
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 460f + i * 60f, new Color(90, 120, 200) * (0.1f * blend), ph + time * 0.02f);
            }
        }

        public override void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottom, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //书脊:银色星芒错落闪烁,各自按不同相位呼吸
            if (spine.Height > 30) {
                for (int i = 0; i < 9; i++) {
                    float y = spine.Y + 14f + (spine.Height - 28f) * CEPortraitDraw.Hash01(i, 2.2f);
                    float x = spine.Center.X + (CEPortraitDraw.Hash01(i, 5.5f) - 0.5f) * spine.Width * 0.5f;
                    float tw = 0.5f + 0.5f * MathF.Sin(time * (1.5f + i * 0.3f) + i * 2.4f);
                    float size = MathF.Min(4f, spine.Width * 0.35f) * (0.5f + 0.5f * tw);
                    CEPortraitDraw.Star(sb, new Vector2(x, y), size, MoonWhite with { A = 0 } * ((0.3f + 0.6f * tw) * blend));
                }
                //脊心一点月色柔光缓慢上下
                float gy = spine.Center.Y + MathF.Sin(time * 0.4f) * (spine.Height * 0.32f);
                CEPortraitDraw.Glow(sb, new Vector2(spine.Center.X, gy), new Vector2(spine.Width * 1.6f, 80f), PhaseCyan * (0.3f * blend));
            }
            //底封边:一道柔光沿边来回巡游,身后拖淡尾
            if (bottom.Height >= 3) {
                float k = 0.5f + 0.5f * MathF.Sin(time * 0.7f);
                float x = bottom.X + bottom.Width * k;
                CEPortraitDraw.Glow(sb, new Vector2(x, bottom.Center.Y), new Vector2(120f, bottom.Height * 3f), MoonWhite * (0.45f * blend));
                CEPortraitDraw.Fill(sb, new Vector2(x - 20f, bottom.Y + 1), new Vector2(40f, Math.Max(1, bottom.Height - 2)), MoonWhite with { A = 0 } * (0.3f * blend));
            }
        }
    }
}
