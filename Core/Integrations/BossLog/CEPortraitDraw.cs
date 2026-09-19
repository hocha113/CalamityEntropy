using CalamityEntropy.Assets.Register;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Core.Integrations.BossLog
{
    /// <summary>
    /// 图鉴沙盒共享绘制图元(场景坐标或 UI 坐标均可,只是 magic-pixel 矢量):
    /// 竖向渐变、椭圆折线环、六边形网格、四芒星点、柔光。演员与主题共用,别各自再写一份
    /// </summary>
    internal static class CEPortraitDraw
    {
        private static readonly Rectangle PixelSrc = new(0, 0, 1, 1);

        public static Texture2D Pixel => VaultAsset.placeholder2?.Value;

        /// <summary>左上角起的实心矩形(浮点位置与尺寸)</summary>
        public static void Fill(SpriteBatch sb, Vector2 topLeft, Vector2 size, Color c) {
            Texture2D px = Pixel;
            if (px == null || size.X <= 0f || size.Y <= 0f) {
                return;
            }
            sb.Draw(px, topLeft, PixelSrc, c, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);
        }

        /// <summary>竖向渐变(分带,不透明)。<paramref name="y0"/> 在上、<paramref name="y1"/> 在下</summary>
        public static void VerticalGradient(SpriteBatch sb, float x0, float x1, float y0, float y1, Color top, Color bottom, int bands = 20) {
            Texture2D px = Pixel;
            if (px == null || bands < 1 || x1 <= x0 || y1 <= y0) {
                return;
            }
            float bandH = (y1 - y0) / bands;
            for (int i = 0; i < bands; i++) {
                float t = bands == 1 ? 0f : i / (float)(bands - 1);
                Color c = Color.Lerp(top, bottom, t);
                sb.Draw(px, new Vector2(x0, y0 + i * bandH), PixelSrc, c, 0f, Vector2.Zero,
                    new Vector2(x1 - x0, bandH + 1f), SpriteEffects.None, 0f);
            }
        }

        public static void Line(SpriteBatch sb, Vector2 a, Vector2 b, float thick, Color c) {
            Texture2D px = Pixel;
            if (px == null) {
                return;
            }
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.5f) {
                return;
            }
            sb.Draw(px, a, PixelSrc, c, d.ToRotation(), new Vector2(0f, 0.5f),
                new Vector2(len, thick), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 椭圆折线环。<paramref name="radii"/>.X 沿 <paramref name="rotation"/> 方向,.Y 垂直。
        /// <paramref name="glintPhase"/> 非 NaN 时在该相位附近 0.6 弧度内叠一段 <paramref name="glint"/> 色亮弧
        /// </summary>
        public static void Ellipse(SpriteBatch sb, Vector2 center, Vector2 radii, float rotation, Color c, float thick,
            int segments = 40, float glintPhase = float.NaN, Color glint = default) {
            if (Pixel == null || segments < 3) {
                return;
            }
            Vector2 prev = center + RimPoint(0f, radii, rotation);
            bool useGlint = !float.IsNaN(glintPhase);
            for (int i = 1; i <= segments; i++) {
                float t = MathHelper.TwoPi * i / segments;
                Vector2 next = center + RimPoint(t, radii, rotation);
                Line(sb, prev, next, thick, c);
                if (useGlint) {
                    float dist = Math.Abs(MathHelper.WrapAngle(t - glintPhase));
                    float hot = MathHelper.Clamp(1f - dist / 0.6f, 0f, 1f);
                    if (hot > 0f) {
                        Line(sb, prev, next, thick * 0.6f, glint * hot);
                    }
                }
                prev = next;
            }
        }

        private static Vector2 RimPoint(float t, Vector2 radii, float rotation)
            => new Vector2(MathF.Cos(t) * radii.X, MathF.Sin(t) * radii.Y).RotatedBy(rotation);

        /// <summary>正六边形轮廓(尖顶朝上)</summary>
        public static void Hex(SpriteBatch sb, Vector2 center, float r, Color c, float thick) {
            Vector2 prev = center + new Vector2(0f, -r);
            for (int i = 1; i <= 6; i++) {
                Vector2 next = center + new Vector2(0f, -r).RotatedBy(MathHelper.Pi / 3f * i);
                Line(sb, prev, next, thick, c);
                prev = next;
            }
        }

        /// <summary>
        /// 铺满矩形区域的六边形网格(尖顶排布,奇数行错半格)。<paramref name="alphaAt"/> 按格心返回 0..1 的强度,
        /// 返回 0 的格跳过不画;<paramref name="offset"/> 让网格整体漂移
        /// </summary>
        public static void HexGrid(SpriteBatch sb, Vector2 min, Vector2 max, float r, Vector2 offset,
            Func<Vector2, float> alphaAt, Color c, float thick) {
            if (Pixel == null || r < 4f) {
                return;
            }
            float dx = MathF.Sqrt(3f) * r;
            float dy = 1.5f * r;
            float ox = offset.X % dx;
            float oy = offset.Y % (dy * 2f);
            int rows = (int)MathF.Ceiling((max.Y - min.Y) / dy) + 3;
            int cols = (int)MathF.Ceiling((max.X - min.X) / dx) + 3;
            for (int row = -2; row < rows; row++) {
                float y = min.Y + oy + row * dy;
                float shift = (row & 1) == 0 ? 0f : dx * 0.5f;
                for (int col = -2; col < cols; col++) {
                    Vector2 center = new(min.X + ox + col * dx + shift, y);
                    if (center.X < min.X - r || center.X > max.X + r || center.Y < min.Y - r || center.Y > max.Y + r) {
                        continue;
                    }
                    float a = alphaAt?.Invoke(center) ?? 1f;
                    if (a <= 0.005f) {
                        continue;
                    }
                    Hex(sb, center, r, c * a, thick);
                }
            }
        }

        /// <summary>四芒星点:两笔正交细线(加色色值由调用方决定)</summary>
        public static void Star(SpriteBatch sb, Vector2 center, float size, Color c, float thick = 1f) {
            Line(sb, center - new Vector2(size, 0f), center + new Vector2(size, 0f), thick, c);
            Line(sb, center - new Vector2(0f, size), center + new Vector2(0f, size), thick, c);
        }

        /// <summary>柔光圆点(Glow2 贴图,A=0 加色读数),<paramref name="size"/> 为直径</summary>
        public static void Glow(SpriteBatch sb, Vector2 center, Vector2 size, Color c) {
            Texture2D g = CEExtraAssets.Glow2;
            if (g == null) {
                return;
            }
            sb.Draw(g, center, null, c with { A = 0 }, 0f, g.Size() * 0.5f,
                new Vector2(size.X / g.Width, size.Y / g.Height), SpriteEffects.None, 0f);
        }

        /// <summary>柔光圆点(等直径)</summary>
        public static void Glow(SpriteBatch sb, Vector2 center, float diameter, Color c)
            => Glow(sb, center, new Vector2(diameter, diameter), c);

        /// <summary>烟羽(Smoke 真 alpha),可直接染色</summary>
        public static void Puff(SpriteBatch sb, Vector2 center, float size, Color c, float rot) {
            Texture2D f = CEExtraAssets.Smoke;
            if (f == null) {
                return;
            }
            sb.Draw(f, center, null, c, rot, f.Size() * 0.5f, size / f.Width, SpriteEffects.None, 0f);
        }

        /// <summary>确定性伪随机(黄金角散布用),返回 0..1</summary>
        public static float Hash01(int i, float salt = 0f) {
            float v = MathF.Sin(i * 12.9898f + salt * 78.233f) * 43758.5453f;
            return v - MathF.Floor(v);
        }
    }
}
