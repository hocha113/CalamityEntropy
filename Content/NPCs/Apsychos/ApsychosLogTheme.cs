using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    /// <summary>
    /// 无魂者的图鉴域主题:灰烬余火。炭黑封面配余烬橙封边、焦褐纸面;
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
