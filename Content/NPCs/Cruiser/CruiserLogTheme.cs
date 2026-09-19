using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    /// <summary>
    /// 巡游者的图鉴域主题:虚空雷暴。深紫罗兰封面配淡雷光封边、暗紫纸面;
    /// 书脊上一条电弧抖动巡游,底封边散落雷火火花
    /// </summary>
    internal sealed class CruiserLogTheme : CEBossLogTheme
    {
        public static readonly CruiserLogTheme Instance = new();

        /// <summary>雷光核色 / 晕色</summary>
        public static readonly Color BoltCore = new(236, 232, 255);
        public static readonly Color BoltHalo = new(150, 120, 255);
        /// <summary>巡游者天幕的基色与加色(与 CrSky 同源)</summary>
        public static readonly Color VoidBase = new(30, 20, 60);
        public static readonly Color VoidAdd = new(66, 60, 94);

        public override Color Cover => new(22, 14, 44);
        public override Color CoverEdge => new(168, 160, 214);
        public override Color Spine => new(14, 9, 30);
        public override Color Paper => new(40, 30, 66);
        public override Color PaperLight => new(170, 160, 215);
        public override Color Rule => new(120, 105, 170);
        public override Color Accent => new(222, 214, 255);
        public override Color Muted => new(170, 160, 200);

        public override void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottom, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //书脊电弧:沿脊心的抖动折线,按 8 帧一换的时间桶取随机,读作持续跳动的弧光
            if (spine.Height > 30) {
                int bucket = (int)(time * 8f);
                float cx = spine.Center.X;
                Vector2 prev = new(cx, spine.Y + 8f);
                int steps = Math.Max(4, spine.Height / 18);
                float step = (spine.Height - 16f) / steps;
                Color core = BoltCore with { A = 0 } * (0.5f * blend);
                Color halo = BoltHalo with { A = 0 } * (0.3f * blend);
                for (int i = 1; i <= steps; i++) {
                    float jitter = (CEPortraitDraw.Hash01(bucket * 31 + i, 5.5f) - 0.5f) * spine.Width * 0.8f;
                    Vector2 next = new(cx + jitter, spine.Y + 8f + i * step);
                    CEPortraitDraw.Line(sb, prev, next, 3f, halo);
                    CEPortraitDraw.Line(sb, prev, next, 1f, core);
                    prev = next;
                }
            }
            //底封边:雷火火花,几点亮白闪烁
            if (bottom.Height >= 3) {
                int bucket = (int)(time * 6f);
                for (int i = 0; i < 12; i++) {
                    float h = CEPortraitDraw.Hash01(bucket * 17 + i, 6.6f);
                    if (h < 0.55f) {
                        continue;
                    }
                    float x = bottom.X + CEPortraitDraw.Hash01(i, 8.8f) * bottom.Width;
                    float y = bottom.Y + 1 + CEPortraitDraw.Hash01(i, 2.7f) * Math.Max(1, bottom.Height - 3);
                    CEPortraitDraw.Fill(sb, new Vector2(x, y), new Vector2(2f, 2f), BoltCore with { A = 0 } * ((h - 0.55f) / 0.45f * 0.8f * blend));
                }
            }
        }

        /// <summary>
        /// 一道雷电折线:自 <paramref name="origin"/> 向两个反方向随机游走各 <paramref name="half"/> 步(与 CrSky 的 LightningBolt 同构),
        /// 全部随机取自 <paramref name="seed"/>,同一 seed 每帧画出同一道;<paramref name="scale"/> 缩放步长
        /// </summary>
        public static void DrawBolt(SpriteBatch sb, int seed, Vector2 origin, float scale, float intensity, int half) {
            Color core = BoltCore with { A = 0 } * intensity;
            Color halo = BoltHalo with { A = 0 } * (0.4f * intensity);
            float a1 = CEPortraitDraw.Hash01(seed, 0.7f) * MathHelper.TwoPi;
            for (int side = 0; side < 2; side++) {
                float ang = a1 + side * MathHelper.Pi;
                Vector2 p = origin;
                for (int i = 0; i < half; i++) {
                    int h = seed * 131 + side * 977 + i;
                    ang += (CEPortraitDraw.Hash01(h, 1.9f) - 0.5f) * 1.1f;
                    Vector2 next = p + ang.ToRotationVector2() * ((22f + CEPortraitDraw.Hash01(h, 3.1f) * 26f) * scale);
                    float fade = 1f - i / (float)half;
                    CEPortraitDraw.Line(sb, p, next, 6f * scale, halo * fade);
                    CEPortraitDraw.Line(sb, p, next, 2f * scale, core * fade);
                    p = next;
                }
            }
        }
    }
}
