using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 卫城机器的图鉴域主题:锈铁余火。锈褐封面配黄铜封边、焦褐纸面;
    /// 书后是烟霾暮空(低垂浊日、余火背光、飘烟、火花上浮),
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
        public override Color Dim => new(12, 8, 6);
        public override Color SkyTop => SmogTop;
        public override Color SkyBottom => new(118, 74, 48);

        public override void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //浊日与余火背光:书左下一轮低垂的浊日,屏幕下沿暗橙背光
            Vector2 sun = new(book.X - 140f, book.Bottom - 40f);
            CEPortraitDraw.Glow(sb, sun, 560f, Ember * (0.26f * blend));
            CEPortraitDraw.Glow(sb, sun, 170f, Brass * (0.42f * blend));
            CEPortraitDraw.Glow(sb, new Vector2(screen.Center.X, screen.Bottom + 80f), new Vector2(screen.Width * 1.3f, 420f), Ember * (0.16f * blend));

            //火花:自下而上飘升的亮粒,带重力回落的抛物弧,忽明忽暗
            for (int i = 0; i < 60; i++) {
                float hx = CEPortraitDraw.Hash01(i, 2.2f);
                float hy = CEPortraitDraw.Hash01(i, 6.6f);
                float life = (time * (0.35f + hx * 0.3f) + hy * 10f) % 1f;
                float x = screen.X + hx * screen.Width + MathF.Sin(life * 6f + i) * 30f * life;
                float y = screen.Bottom + 20f - (screen.Height * 0.7f) * life * (2f - life) * 0.75f;
                if (book.Contains((int)x, (int)y)) {
                    continue;
                }
                float flick = 0.55f + 0.45f * MathF.Sin(time * (7f + hx * 9f) + i);
                float a = (1f - life) * flick * blend;
                Color c = Color.Lerp(Ember, Brass, hy * 0.6f);
                CEPortraitDraw.Fill(sb, new Vector2(x, y), new Vector2(1.6f + hx, 1.6f + hx), c with { A = 0 } * a);
            }

            //飘烟:屏幕下沿几团暗灰烟羽缓移
            for (int i = 0; i < 5; i++) {
                float ph = i * 1.5f;
                float x = (time * (11f + i * 3f) + ph * 230f) % (screen.Width + 640f) - 320f;
                float y = screen.Bottom - 90f - 50f * MathF.Sin(ph);
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 480f + i * 50f, Smoke * (0.12f * blend), ph + time * 0.03f);
            }

            //烟霾横流:几条暗色宽带缓慢横移,读作低空烟层
            for (int i = 0; i < 4; i++) {
                float ph = i * 97.3f;
                float len = 260f + i % 2 * 120f;
                float x = (time * (14f + i * 4f) + ph * 3f) % (screen.Width + len * 2f) - len;
                float y = screen.Y + screen.Height * (0.25f + i * 0.17f);
                float a = (0.06f + 0.03f * MathF.Sin(time * 0.5f + ph)) * blend;
                CEPortraitDraw.Line(sb, new Vector2(x, y), new Vector2(x + len, y + 6f), 18f, Soot * a);
            }
        }

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
