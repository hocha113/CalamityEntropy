using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    /// <summary>
    /// 巡游者的图鉴域主题:虚空雷暴。深紫罗兰封面配淡雷光封边、暗紫纸面;
    /// 书后是巡游者天幕的紫灰虚空(两团星云背光 + 漂浮虚空尘),不时有一道雷电劈过整屏;
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
        public override Color Dim => new(6, 4, 14);
        public override Color SkyTop => VoidBase;
        public override Color SkyBottom => VoidAdd;

        public override void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend) {
            if (CEPortraitDraw.Pixel == null) {
                return;
            }
            //星云背光
            CEPortraitDraw.Glow(sb, new Vector2(book.X - 120f, book.Y - 40f), 620f, VoidAdd * (0.5f * blend));
            CEPortraitDraw.Glow(sb, new Vector2(book.Right + 160f, book.Bottom + 40f), 520f, new Color(90, 60, 150) * (0.35f * blend));

            //虚空尘:淡紫小方块缓慢上浮
            for (int i = 0; i < 60; i++) {
                float hx = CEPortraitDraw.Hash01(i, 3.3f);
                float hy = CEPortraitDraw.Hash01(i, 9.9f);
                float rise = (time * (10f + hx * 14f) + hy * 900f) % (screen.Height + 40f);
                Vector2 p = new(screen.X + hx * screen.Width + MathF.Sin(time * 0.4f + i) * 12f, screen.Bottom + 20f - rise);
                if (book.Contains((int)p.X, (int)p.Y)) {
                    continue;
                }
                float a = (0.18f + 0.22f * MathF.Sin(time * 1.3f + i * 2.1f)) * blend;
                float s = 1.5f + hy * 2f;
                CEPortraitDraw.Fill(sb, p, new Vector2(s, s), BoltHalo with { A = 0 } * a);
            }

            //整屏雷电:每 2.6 秒一桶,桶首 0.45 秒内闪过一道折线,亮度平方衰减;整屏随之微亮
            const float Period = 2.6f;
            int bucket = (int)(time / Period);
            float local = time - bucket * Period;
            if (local < 0.45f && CEPortraitDraw.Hash01(bucket, 4.4f) > 0.25f) {
                float k = 1f - local / 0.45f;
                float intensity = k * k * blend;
                Vector2 origin = new(screen.X + CEPortraitDraw.Hash01(bucket, 1.1f) * screen.Width,
                    screen.Y + CEPortraitDraw.Hash01(bucket, 2.2f) * screen.Height * 0.7f);
                CEPortraitDraw.Fill(sb, new Vector2(screen.X, screen.Y), new Vector2(screen.Width, screen.Height), BoltHalo with { A = 0 } * (0.08f * intensity));
                DrawBolt(sb, bucket, origin, 1f, intensity, 20);
            }
        }

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
