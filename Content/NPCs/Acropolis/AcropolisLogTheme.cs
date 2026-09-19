using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 卫城机器的图鉴域主题:锈铁余火。锈褐封面配黄铜封边、焦褐纸面;
    /// 书脊上一列铆钉随一道高光顺次亮起,底封边火花四溅
    /// </summary>
    internal sealed class AcropolisLogTheme : CEBossLogTheme
    {
        public static readonly AcropolisLogTheme Instance = new();

        /// <summary>机体环境光(暖白偏橙,读作被余火照着的铁壳)</summary>
        public static readonly Color MachineAmbient = new(236, 218, 200);
        public static readonly Color Ember = new(255, 150, 60);
        public static readonly Color Brass = new(220, 170, 90);
        public static readonly Color Soot = new(50, 40, 38);
        public static readonly Color Smoke = new(120, 104, 96);
        public static readonly Color DustWarm = new(150, 112, 78);
        public static readonly Color DustDark = new(84, 60, 44);
        /// <summary>沙盒场景:烟霾天幕两端、残柱、焦土</summary>
        public static readonly Color SmogTop = new(38, 26, 24);
        public static readonly Color SmogBottom = new(104, 68, 46);
        public static readonly Color Ruin = new(30, 20, 16);
        public static readonly Color RuinLit = new(70, 44, 30);
        public static readonly Color SoilTop = new(92, 64, 44);
        public static readonly Color SoilBottom = new(38, 24, 16);

        public override Color Cover => new(58, 34, 24);
        public override Color CoverEdge => new(196, 144, 74);
        public override Color Spine => new(40, 24, 16);
        public override Color Paper => new(64, 46, 36);
        public override Color PaperLight => new(204, 164, 112);
        public override Color Rule => new(152, 106, 62);
        public override Color Accent => new(255, 176, 88);
        public override Color Muted => new(204, 174, 136);

        public override void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottom, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //书脊铆钉:等距一列黄铜小方钉,一道高光自上而下顺次扫亮
            if (spine.Height > 30) {
                float run = (time * 70f) % (spine.Height + 80f) - 40f;
                float w = MathF.Min(4f, spine.Width * 0.4f);
                for (float y = spine.Y + 10f; y < spine.Bottom - 10f; y += 13f) {
                    float d = MathF.Abs(y - (spine.Y + run));
                    float hot = MathHelper.Clamp(1f - d / 50f, 0f, 1f);
                    Color c = Color.Lerp(Brass * 0.55f, Ember, hot) with { A = 0 } * ((0.35f + 0.6f * hot) * blend);
                    CEPortraitDraw.Fill(sb, new Vector2(spine.Center.X - w * 0.5f, y), new Vector2(w, w), c);
                }
            }
            //底封边:火花四溅,零星亮起、迅速熄灭
            if (bottom.Height >= 3) {
                int bucket = (int)(time * 8f);
                int h = Math.Max(1, bottom.Height - 2);
                for (int i = 0; i < 14; i++) {
                    float hh = CEPortraitDraw.Hash01(bucket * 23 + i, 3.7f);
                    if (hh < 0.6f) {
                        continue;
                    }
                    float x = bottom.X + CEPortraitDraw.Hash01(bucket * 7 + i, 9.4f) * bottom.Width;
                    float y = bottom.Y + 1 + CEPortraitDraw.Hash01(i, 1.9f) * h;
                    Color c = Color.Lerp(Ember, Color.White, (hh - 0.6f) / 0.4f) with { A = 0 };
                    CEPortraitDraw.Fill(sb, new Vector2(x, y), new Vector2(3f, 1.5f), c * ((hh - 0.6f) / 0.4f * 0.9f * blend));
                }
            }
        }
    }
}
