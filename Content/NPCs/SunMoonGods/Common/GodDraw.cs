using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Common
{
    /// <summary>
    /// 日月双神共用的绘制小件,进出批次都是 Deferred/AlphaBlend
    /// 发光层一律进加法批次,白底真透明贴图在 AlphaBlend 里会糊成实心盘
    /// 黑底贴图(Corona、BloomRing、BrightFlash、StarTexture)画不出暗色
    /// </summary>
    internal static class GodDraw
    {
        private static ColoredVertex[] buffer = new ColoredVertex[256];

        public static Texture2D Glow => CEUtils.getExtraTex("Glow");
        public static Texture2D SoftLight => CEUtils.getExtraTex("light");
        public static Texture2D BloomRing => CEUtils.getExtraTex("BloomRing");
        public static Texture2D Corona => CEUtils.getExtraTex("Corona");
        public static Texture2D Flash => CEUtils.getExtraTex("BrightFlash");
        public static Texture2D Star => CEUtils.getExtraTex("StarTexture");
        public static Texture2D StarSolid => CEUtils.getExtraTex("StarTexture_White");
        public static Texture2D ThinStar => CEUtils.getExtraTex("Star2");
        public static Texture2D Disc => CEUtils.getExtraTex("BasicCircle");
        public static Texture2D SoftLine => CEUtils.getExtraTex("BasicTrailThin");
        public static Texture2D FireStreak => CEUtils.getExtraTex("StreakFire");
        /// <summary>原版纺锤光,真透明,暗色也能画</summary>
        public static Texture2D Spindle => TextureAssets.Extra[98].Value;

        /// <summary>BloomRing 可见环带直径占贴图宽的比例,按它折算缩放</summary>
        public const float BloomRingVisible = 0.85f;

        /// <summary>加法 Immediate 批次,精灵和三角条都能画</summary>
        public static void BeginAdditive() => Main.spriteBatch.UseAdditive();

        /// <summary>直通道透明批次,白底真透明贴图染成实体用它</summary>
        public static void BeginStraightAlpha() => Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearClamp);

        public static void End() => CEUtils.ReSetToEndShader();

        /// <summary>
        /// 加法批次用的染色:只缩 RGB,A 留 255
        /// 加法混合是源色乘源 alpha,直接 Color * k 会把强度乘成 k 的平方
        /// </summary>
        public static Color Add(Color color, float intensity) {
            intensity = MathHelper.Clamp(intensity, 0f, 4f);
            return new Color(
                (int)Math.Min(255f, color.R * intensity),
                (int)Math.Min(255f, color.G * intensity),
                (int)Math.Min(255f, color.B * intensity),
                255);
        }

        /// <summary>直通道批次用的染色:RGB 不动,透明度只写进 A</summary>
        public static Color Straight(Color color, float alpha) {
            return new Color(color.R, color.G, color.B, (int)(255f * MathHelper.Clamp(alpha, 0f, 1f)));
        }

        /// <summary>环贴图等比缩放到指定半径时的 scale</summary>
        public static float RingScale(float radius) => radius * 2f / (BloomRing.Width * BloomRingVisible);

        /// <summary>中心对齐画一张贴图,pos 是世界坐标</summary>
        public static void Centered(Texture2D tex, Vector2 worldPos, Color color, float rotation, float scale) {
            Main.spriteBatch.Draw(tex, worldPos - Main.screenPosition, null, color, rotation, tex.Size() / 2f, scale, SpriteEffects.None, 0f);
        }

        public static void Centered(Texture2D tex, Vector2 worldPos, Color color, float rotation, Vector2 scale) {
            Main.spriteBatch.Draw(tex, worldPos - Main.screenPosition, null, color, rotation, tex.Size() / 2f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 沿折线画三角条,u 沿线,v 跨线,points 是世界坐标
        /// 调用前批次须是 Immediate,BeginAdditive 和 BeginStraightAlpha 都满足
        /// </summary>
        /// <param name="halfWidth">参数是 0 到 1 的沿线进度</param>
        public static void Strip(Texture2D tex, IReadOnlyList<Vector2> points, Func<float, float> halfWidth, Func<float, Color> color,
            float uFrom = 0f, float uTo = 1f) {
            int n = points.Count;
            if (n < 2) {
                return;
            }
            EnsureBuffer(n * 2);
            for (int i = 0; i < n; i++) {
                float t = i / (float)(n - 1);
                Vector2 along = i == 0 ? points[1] - points[0] : i == n - 1 ? points[n - 1] - points[n - 2] : points[i + 1] - points[i - 1];
                Vector2 normal = along.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                float w = halfWidth(t);
                Color c = color(t);
                Vector2 p = points[i] - Main.screenPosition;
                float u = MathHelper.Lerp(uFrom, uTo, t);
                buffer[i * 2] = new ColoredVertex(p + normal * w, c, new Vector3(u, 0f, 1f));
                buffer[i * 2 + 1] = new ColoredVertex(p - normal * w, c, new Vector3(u, 1f, 1f));
            }
            Flush(tex, n * 2);
        }

        /// <summary>
        /// 两端渐隐的直线光带,整条拉伸的线贴图两端会平切
        /// 调用前须在 BeginAdditive 的批次里
        /// </summary>
        /// <param name="capFraction">每端渐隐段占全长的比例</param>
        public static void Line(Vector2 start, Vector2 end, float halfWidth, Color color, float capFraction = 0.12f, Texture2D tex = null) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f || halfWidth <= 0f) {
                return;
            }
            Vector2 dir = diff / length;
            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2) * halfWidth;
            Vector2 s = start - Main.screenPosition;
            EnsureBuffer(8);
            float cap = MathHelper.Clamp(capFraction, 0f, 0.5f);
            WriteCross(0, s, normal, Color.Transparent, 0f);
            WriteCross(2, s + dir * (length * cap), normal, color, cap);
            WriteCross(4, s + dir * (length * (1f - cap)), normal, color, 1f - cap);
            WriteCross(6, s + dir * length, normal, Color.Transparent, 1f);
            Flush(tex ?? SoftLine, 8);
        }

        /// <summary>等宽圆环三角条,正圆和有缺口的弧都用它,调用前须在 Immediate 批次里</summary>
        /// <param name="arc">弧长,单位是弧度,从 startAngle 起算</param>
        public static void RingStrip(Texture2D tex, Vector2 center, float radius, float halfWidth, Color color,
            float startAngle = 0f, float arc = MathHelper.TwoPi, int segments = 48, float uRepeat = 1f) {
            if (radius <= 0f || halfWidth <= 0f || segments < 3) {
                return;
            }
            EnsureBuffer((segments + 1) * 2);
            Vector2 c = center - Main.screenPosition;
            for (int i = 0; i <= segments; i++) {
                float t = i / (float)segments;
                Vector2 dir = (startAngle + arc * t).ToRotationVector2();
                buffer[i * 2] = new ColoredVertex(c + dir * (radius + halfWidth), color, new Vector3(t * uRepeat, 0f, 1f));
                buffer[i * 2 + 1] = new ColoredVertex(c + dir * (radius - halfWidth), color, new Vector3(t * uRepeat, 1f, 1f));
            }
            Flush(tex, (segments + 1) * 2);
        }

        private static void WriteCross(int index, Vector2 center, Vector2 normal, Color color, float u) {
            buffer[index] = new ColoredVertex(center + normal, color, new Vector3(u, 0f, 1f));
            buffer[index + 1] = new ColoredVertex(center - normal, color, new Vector3(u, 1f, 1f));
        }

        private static void EnsureBuffer(int count) {
            if (buffer.Length < count) {
                buffer = new ColoredVertex[Math.Max(count, buffer.Length * 2)];
            }
        }

        private static void Flush(Texture2D tex, int vertexCount) {
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.Textures[0] = tex;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, buffer, 0, vertexCount - 2);
        }

        public static float EaseOut(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - (1f - t) * (1f - t);
        }

        public static float EaseIn(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t;
        }

        public static float Smooth(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>from 到 to 之间线性归一并钳到 0..1</summary>
        public static float Span(float value, float from, float to) => Utils.GetLerpValue(from, to, value, true);
    }
}