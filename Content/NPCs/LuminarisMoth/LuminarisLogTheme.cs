using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth
{
    /// <summary>
    /// 月华之蛾的图鉴域主题:月夜银翼。午夜靛蓝封面配银封边、靛蓝纸面;
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
