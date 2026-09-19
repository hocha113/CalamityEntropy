using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    /// <summary>
    /// 虚无双子的图鉴域主题:深渊虚无。深海军蓝封面配淡青封边、蓝黑纸面;
    /// 书后是虚无双子天幕的深蓝水体(孢子群上浮、空心细胞环缓漂、深蓝背光),
    /// 书脊上两股螺旋点链缓缓上旋(噬菌体母题),底封边一排细胞小环顺次亮起
    /// </summary>
    internal sealed class NihilityLogTheme : CEBossLogTheme
    {
        public static readonly NihilityLogTheme Instance = new();

        /// <summary>天幕深蓝(与 NihTwinSky 同源)/ 孢子青 / 细胞紫</summary>
        public static readonly Color Abyss = new(0, 10, 60);
        public static readonly Color SporeCyan = new(120, 220, 255);
        public static readonly Color CellViolet = new(150, 120, 255);

        public override Color Cover => new(6, 14, 40);
        public override Color CoverEdge => new(110, 200, 230);
        public override Color Spine => new(4, 9, 28);
        public override Color Paper => new(18, 30, 60);
        public override Color PaperLight => new(120, 180, 220);
        public override Color Rule => new(70, 130, 170);
        public override Color Accent => new(150, 235, 255);
        public override Color Muted => new(140, 175, 205);
        public override Color Dim => new(2, 5, 14);
        public override Color SkyTop => Abyss;
        public override Color SkyBottom => new(10, 40, 90);

        public override void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //深蓝背光两团
            CEPortraitDraw.Glow(sb, new Vector2(book.X - 100f, book.Bottom + 80f), 640f, new Color(20, 60, 140) * (0.45f * blend));
            CEPortraitDraw.Glow(sb, new Vector2(book.Right + 150f, book.Y - 60f), 520f, new Color(60, 40, 140) * (0.35f * blend));

            //孢子群:柔光小点上浮、左右摇摆,越靠上越淡
            for (int i = 0; i < 70; i++) {
                float hx = CEPortraitDraw.Hash01(i, 2.4f);
                float hy = CEPortraitDraw.Hash01(i, 8.1f);
                float rise = (time * (14f + hx * 18f) + hy * 1300f) % (screen.Height + 60f);
                float y = screen.Bottom + 30f - rise;
                float x = screen.X + hx * screen.Width + MathF.Sin(time * 0.6f + i * 1.3f) * 18f;
                if (book.Contains((int)x, (int)y)) {
                    continue;
                }
                float fade = MathHelper.Clamp(rise / screen.Height, 0f, 1f);
                float a = (0.5f - 0.35f * fade) * blend;
                Color c = Color.Lerp(SporeCyan, CellViolet, hy);
                CEPortraitDraw.Glow(sb, new Vector2(x, y), 10f + hx * 10f, c * a);
            }

            //空心细胞环:大小不一的椭圆轮廓缓慢横漂、轻微呼吸
            for (int i = 0; i < 9; i++) {
                float ph = i * 41.3f;
                float r = 26f + i % 4 * 14f;
                float x = (time * (7f + i * 2f) + ph * 23f) % (screen.Width + 200f) - 100f;
                float y = screen.Y + (ph * 3.7f) % screen.Height;
                if (book.Contains((int)x, (int)y)) {
                    continue;
                }
                float breath = 1f + 0.08f * MathF.Sin(time * 1.1f + ph);
                CEPortraitDraw.Ellipse(sb, new Vector2(x, y), new Vector2(r * breath, r * 0.8f * breath), ph * 0.1f,
                    CellViolet with { A = 0 } * (0.14f * blend), 1.5f, 28);
            }
        }

        public override void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottom, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //书脊双螺旋:两股正弦点链,相位随时间上旋
            if (spine.Height > 30) {
                float cx = spine.Center.X;
                float amp = spine.Width * 0.3f;
                for (float y = spine.Y + 6; y < spine.Bottom - 6; y += 5f) {
                    float ph = y * 0.09f - time * 2.2f;
                    float x1 = cx + MathF.Sin(ph) * amp;
                    float x2 = cx + MathF.Sin(ph + MathHelper.Pi) * amp;
                    float depth1 = 0.5f + 0.5f * MathF.Cos(ph);
                    float depth2 = 1f - depth1;
                    CEPortraitDraw.Fill(sb, new Vector2(x1 - 1f, y), new Vector2(2f, 2f), SporeCyan with { A = 0 } * ((0.25f + 0.5f * depth1) * blend));
                    CEPortraitDraw.Fill(sb, new Vector2(x2 - 1f, y), new Vector2(2f, 2f), CellViolet with { A = 0 } * ((0.25f + 0.5f * depth2) * blend));
                }
            }
            //底封边细胞小环:等距一排,一段亮度顺次点亮
            if (bottom.Height >= 5) {
                float r = MathF.Min(3f, bottom.Height * 0.4f);
                float run = (time * 120f) % (bottom.Width + 160f) - 80f;
                for (float x = bottom.X + 10; x < bottom.Right - 10; x += 26f) {
                    float d = MathF.Abs(x - (bottom.X + run));
                    float hot = MathHelper.Clamp(1f - d / 110f, 0f, 1f);
                    Color c = Color.Lerp(CellViolet, SporeCyan, hot) with { A = 0 } * ((0.25f + 0.6f * hot) * blend);
                    CEPortraitDraw.Ellipse(sb, new Vector2(x, bottom.Center.Y), new Vector2(r, r), 0f, c, 1f, 12);
                }
            }
        }
    }
}
