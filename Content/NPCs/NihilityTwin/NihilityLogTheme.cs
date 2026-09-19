using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    /// <summary>
    /// 虚无双子的图鉴域主题:深渊虚无。深海军蓝封面配淡青封边、蓝黑纸面;
    /// 书脊上两股螺旋点链缓缓上旋(噬菌体母题),底封边一排细胞小环顺次亮起
    /// </summary>
    internal sealed class NihilityLogTheme : CEBossLogTheme
    {
        public static readonly NihilityLogTheme Instance = new();

        /// <summary>孢子青 / 细胞紫</summary>
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
