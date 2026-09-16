using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;

namespace CalamityEntropy.Core.Graphics
{
    /// <summary>
    /// 自研图元拖尾渲染器, 行为等效替代灾厄图元渲染器在本仓库被用到的子集。
    /// 用面(实测 54 处调用): RenderTrail(点列, 设置, 点数), 平滑开/关, 宽度/颜色/偏移委托, MiscShaderData 着色。
    /// 未实现(仓库零调用): 像素化管线, 端帽, 线框调试, 自定义纹理坐标模式, miter 接角, 非默认拓扑。
    /// 输入点为世界坐标, 内部自动减去 Main.screenPosition。
    /// </summary>
    public static class CEPrimitiveRenderer
    {
        private const int MaxPositions = 1000;
        private const float Epsilon = 1e-6f;
        private const string FallbackShaderKey = "CalamityEntropy:StandardPrimitiveShader";

        private static readonly Vector2[] points = new Vector2[MaxPositions];
        private static readonly float[] ratios = new float[MaxPositions];
        private static readonly Vector2[] tangents = new Vector2[MaxPositions];
        private static readonly Vector2[] normals = new Vector2[MaxPositions];
        private static readonly CEPrimitiveVertex[] vertices = new CEPrimitiveVertex[MaxPositions * 2];
        private static readonly int[] validIndices = new int[MaxPositions];
        private static readonly List<Vector2> controlCache = new(MaxPositions);

        // 与原实现一致: 关背面剔除并按屏幕矩形做剪裁
        private static readonly RasterizerState cullNoneScissor = new() {
            CullMode = CullMode.None,
            ScissorTestEnable = true,
        };

        private static int pointCount;
        private static int vertexCount;

        public static void RenderTrail(List<Vector2> positions, CEPrimitiveSettings settings, int? pointsToCreate = null)
            => RenderTrail(positions.ToArray(), settings, pointsToCreate);

        public static void RenderTrail(Vector2[] positions, CEPrimitiveSettings settings, int? pointsToCreate = null) {
            // 点数不足或超限直接放弃, 与原实现一致
            if (positions.Length <= 2 || positions.Length > MaxPositions)
                return;

            int desired = Math.Clamp(pointsToCreate ?? positions.Length, 2, MaxPositions);
            if (!BuildPoints(positions, settings, desired))
                return;
            if (pointCount <= 2)
                return;

            BuildCompletionRatios();
            BuildVertices(settings);
            if (vertexCount <= 3)
                return;

            Render(settings);
        }

        /// <summary>把输入点整理为屏幕坐标点列: 过滤零点, 按需平滑重采样, 应用偏移委托。</summary>
        private static bool BuildPoints(Vector2[] positions, CEPrimitiveSettings settings, int desired) {
            pointCount = 0;

            if (!settings.Smoothen) {
                // 非平滑: 过滤零点后按索引线性重采样到目标点数
                int validCount = 0;
                for (int i = 0; i < positions.Length; i++) {
                    if (positions[i] == Vector2.Zero)
                        continue;
                    validIndices[validCount++] = i;
                }
                if (validCount <= 2)
                    return false;

                int last = validCount - 1;
                float step = 1f / (desired - 1);
                for (int i = 0; i < desired; i++) {
                    float ratio = i * step;
                    float scaled = ratio * last;
                    int cur = (int)scaled;
                    int next = Math.Min(cur + 1, last);
                    Vector2 world = Vector2.Lerp(positions[validIndices[cur]], positions[validIndices[next]], scaled - cur);
                    Vector2 final = world - Main.screenPosition;
                    if (settings.OffsetFunction != null)
                        final += settings.OffsetFunction(ratio, world);
                    points[pointCount++] = final;
                }
                return true;
            }

            // 平滑: 先建控制点(零点剔除+偏移), 再沿 Catmull-Rom 曲线重采样
            controlCache.Clear();
            for (int i = 0; i < positions.Length; i++) {
                if (positions[i] == Vector2.Zero)
                    continue;
                Vector2 offset = -Main.screenPosition;
                if (settings.OffsetFunction != null)
                    offset += settings.OffsetFunction(i / (float)positions.Length, positions[i]);
                controlCache.Add(positions[i] + offset);
            }

            int count = controlCache.Count;
            if (count <= 1) {
                controlCache.Clear();
                return false;
            }

            float lastIndex = count - 1f;
            for (int j = 0; j < desired && pointCount < MaxPositions - 1; j++) {
                float onCurve = j / (float)desired * lastIndex;
                int idx = (int)onCurve;
                float t = onCurve - idx;
                Vector2 p0 = controlCache[Math.Max(idx - 1, 0)];
                Vector2 p1 = controlCache[idx];
                Vector2 p2 = controlCache[Math.Min(idx + 1, count - 1)];
                Vector2 p3 = controlCache[Math.Min(idx + 2, count - 1)];
                points[pointCount++] = Vector2.CatmullRom(p0, p1, p2, p3, MathHelper.Clamp(t, 0f, 1f));
            }
            points[pointCount++] = controlCache[count - 1];
            controlCache.Clear();
            return true;
        }

        /// <summary>按弧长累计并归一化每个点的拖尾进度(0-1)。</summary>
        private static void BuildCompletionRatios() {
            float total = 0f;
            ratios[0] = 0f;
            for (int i = 1; i < pointCount; i++) {
                total += Vector2.Distance(points[i], points[i - 1]);
                ratios[i] = total;
            }

            if (total > Epsilon) {
                float inverse = 1f / total;
                for (int i = 1; i < pointCount; i++)
                    ratios[i] *= inverse;
                ratios[pointCount - 1] = 1f;
            }
            else {
                for (int i = 1; i < pointCount; i++)
                    ratios[i] = 0f;
            }
        }

        /// <summary>逐点求切线与法线。法线用平行传输沿链传播, 避免急转处翻面。</summary>
        private static void BuildFrames() {
            Vector2 fallback = Vector2.UnitX;
            for (int i = 0; i < pointCount; i++) {
                Vector2 tangent = ComputeTangent(i, fallback).SafeNormalize(Vector2.UnitX);
                tangents[i] = tangent;
                fallback = tangent;
            }

            Vector2 prevNormal = Vector2.Zero;
            for (int i = 0; i < pointCount; i++) {
                Vector2 tangent = tangents[i];
                Vector2 baseNormal = new(-tangent.Y, tangent.X);
                Vector2 normal;

                if (i > 0 && prevNormal.LengthSquared() > Epsilon) {
                    // 平行传输: 上一法线按相邻切线的夹角旋转
                    Vector2 prevTangent = tangents[i - 1];
                    float cos = MathHelper.Clamp(Vector2.Dot(prevTangent, tangent), -1f, 1f);
                    float sin = prevTangent.X * tangent.Y - prevTangent.Y * tangent.X;
                    normal = new Vector2(cos * prevNormal.X - sin * prevNormal.Y, sin * prevNormal.X + cos * prevNormal.Y);
                }
                else
                    normal = baseNormal;

                if (normal.LengthSquared() <= Epsilon)
                    normal = baseNormal;

                normal = normal.SafeNormalize(Vector2.UnitY);
                normals[i] = normal;
                prevNormal = normal;
            }
        }

        private static Vector2 ComputeTangent(int index, Vector2 fallback) {
            int last = pointCount - 1;
            Vector2 tangent;

            if (pointCount <= 1)
                tangent = fallback;
            else if (index <= 0)
                tangent = points[1] - points[0];
            else if (index >= last)
                tangent = points[last] - points[last - 1];
            else {
                // 内点取前后差分之和; 折返导致抵消时取较长的一侧
                Vector2 forward = points[index + 1] - points[index];
                Vector2 backward = points[index] - points[index - 1];
                tangent = forward + backward;
                if (tangent.LengthSquared() <= Epsilon)
                    tangent = forward.LengthSquared() >= backward.LengthSquared() ? forward : backward;
            }

            if (tangent.LengthSquared() <= Epsilon)
                tangent = fallback.LengthSquared() > Epsilon ? fallback : Vector2.UnitX;

            return tangent;
        }

        /// <summary>把点列扩展成左右成对的三角带顶点。</summary>
        private static void BuildVertices(CEPrimitiveSettings settings) {
            vertexCount = 0;
            BuildFrames();

            for (int i = 0; i < pointCount; i++) {
                float ratio = ratios[i];
                Vector2 pos = points[i];
                float halfWidth = Math.Max(settings.WidthFunction(ratio, pos), 0f);
                Color color = settings.ColorFunction(ratio, pos);
                float u = MathHelper.Clamp(ratio, 0f, 1f);

                Vector2 left, right;
                float effectiveHalfWidth;
                if (halfWidth <= 0f) {
                    left = pos;
                    right = pos;
                    effectiveHalfWidth = Epsilon;
                }
                else {
                    Vector2 normal = normals[i];
                    // 内点做平滑接角: 与前后法线取平均
                    if (i > 0 && i < pointCount - 1 && pointCount > 2)
                        normal = ((normals[i - 1] + normal + normals[i + 1]) / 3f).SafeNormalize(normal);

                    Vector2 offset = normal * halfWidth;
                    left = pos - offset;
                    right = pos + offset;
                    effectiveHalfWidth = Math.Max(halfWidth, Epsilon);
                }

                // 纵向 uv 编码半宽到 z 分量, 供着色器用 (y-0.5)/z+0.5 还原 0-1 区间
                vertices[vertexCount++] = new CEPrimitiveVertex(left, color, new Vector2(u, 0.5f - effectiveHalfWidth * 0.5f), effectiveHalfWidth);
                vertices[vertexCount++] = new CEPrimitiveVertex(right, color, new Vector2(u, 0.5f + effectiveHalfWidth * 0.5f), effectiveHalfWidth);
            }
        }

        private static void Render(CEPrimitiveSettings settings) {
            GraphicsDevice device = Main.instance.GraphicsDevice;
            device.RasterizerState = cullNoneScissor;
            // 裁剪矩形取真实视口而非 Main.screenWidth/Height:
            // 背景绘制窗口内后者被背景缩放预除,天空里画图元会被裁到左上角;常规绘制两者相等
            device.ScissorRectangle = new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height);

            CalculatePerspectiveMatrices(out Matrix view, out Matrix projection);

            MiscShaderData shader = settings.Shader ?? GameShaders.Misc[FallbackShaderKey];
            shader.Shader.Parameters["uWorldViewProjection"]?.SetValue(view * projection);
            shader.Apply();

            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertexCount - 2);
        }

        /// <summary>屏幕空间正交投影, 含缩放与反重力翻转。</summary>
        public static void CalculatePerspectiveMatrices(out Matrix view, out Matrix projection) {
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Matrix zoomScale = Matrix.CreateScale(zoom.X, zoom.Y, 1f);
            int width = Main.instance.GraphicsDevice.Viewport.Width;
            int height = Main.instance.GraphicsDevice.Viewport.Height;

            view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up)
                * Matrix.CreateTranslation(0f, -height, 0f)
                * Matrix.CreateRotationZ(MathHelper.Pi);
            if (Main.LocalPlayer.gravDir == -1f)
                view *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);
            view *= zoomScale;

            projection = Matrix.CreateOrthographicOffCenter(0f, width * zoom.X, 0f, height * zoom.Y, 0f, 1f) * zoomScale;
        }
    }

    /// <summary>
    /// 二维图元顶点: 位置(Vector2) + 颜色 + 三维纹理坐标(z 存该点半宽, 供着色器还原纵向 uv)。
    /// </summary>
    internal readonly struct CEPrimitiveVertex : IVertexType
    {
        public readonly Vector2 Position;
        public readonly Color Color;
        public readonly Vector3 TextureCoordinates;

        public VertexDeclaration VertexDeclaration => Declaration;

        public static readonly VertexDeclaration Declaration = new(new VertexElement[]
        {
            new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),
        });

        public CEPrimitiveVertex(Vector2 position, Color color, Vector2 textureCoordinates, float halfWidth) {
            Position = position;
            Color = color;
            TextureCoordinates = new Vector3(textureCoordinates, halfWidth);
        }
    }
}
