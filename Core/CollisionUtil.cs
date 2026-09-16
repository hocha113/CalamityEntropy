using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalamityEntropy.Core
{
    public class GeometryShape
    {
        private List<Vector2> vertices;
        private List<Vector2> edges;
        private Vector2 position;

        /// <summary>
        /// 图形的顶点列表（局部坐标）
        /// </summary>
        public List<Vector2> Vertices => vertices;

        /// <summary>
        /// 图形的边向量列表
        /// </summary>
        public List<Vector2> Edges => edges;

        /// <summary>
        /// 图形的位置（世界坐标）
        /// </summary>
        public Vector2 Position {
            get => position;
            set => position = value;
        }

        /// <summary>
        /// 使用顶点列表创建一个几何图形
        /// </summary>
        /// <param name="vertices">顶点列表（局部坐标）</param>
        public GeometryShape(List<Vector2> vertices) {
            this.vertices = new List<Vector2>(vertices);
            CalculateEdges();
            position = Vector2.Zero;
        }

        /// <summary>
        /// 使用顶点列表和位置创建一个几何图形
        /// </summary>
        /// <param name="vertices">顶点列表（局部坐标）</param>
        /// <param name="position">图形位置（世界坐标）</param>
        public GeometryShape(List<Vector2> vertices, Vector2 position) {
            this.vertices = new List<Vector2>(vertices);
            this.position = position;
            CalculateEdges();
        }

        /// <summary>
        /// 计算所有边的向量
        /// </summary>
        private void CalculateEdges() {
            edges = new List<Vector2>();

            for (int i = 0; i < vertices.Count; i++) {
                Vector2 edge = vertices[(i + 1) % vertices.Count] - vertices[i];
                edges.Add(edge);
            }
        }

        /// <summary>
        /// 获取世界坐标系中的顶点位置
        /// </summary>
        /// <returns>转换后的顶点列表</returns>
        public List<Vector2> GetWorldVertices() {
            return vertices.Select(v => v + position).ToList();
        }

        /// <summary>
        /// 检查两个几何图形是否碰撞（使用分离轴定理SAT）
        /// </summary>
        /// <param name="other">另一个几何图形</param>
        /// <returns>如果碰撞返回true，否则返回false</returns>
        public bool CheckCollision(GeometryShape other) {
            List<Vector2> axes = new List<Vector2>();

            // 获取当前图形的法线轴
            foreach (Vector2 edge in edges) {
                axes.Add(new Vector2(-edge.Y, edge.X));
            }

            // 获取另一个图形的法线轴
            foreach (Vector2 edge in other.edges) {
                axes.Add(new Vector2(-edge.Y, edge.X));
            }

            // 归一化所有轴
            for (int i = 0; i < axes.Count; i++) {
                axes[i] = Vector2.Normalize(axes[i]);
            }

            // 对每个轴进行投影测试
            foreach (Vector2 axis in axes) {
                // 跳过零向量轴
                if (axis.LengthSquared() < float.Epsilon)
                    continue;

                // 计算当前图形在轴上的投影
                float minA = float.MaxValue;
                float maxA = float.MinValue;
                foreach (Vector2 vertex in GetWorldVertices()) {
                    float projection = Vector2.Dot(vertex, axis);
                    minA = Math.Min(minA, projection);
                    maxA = Math.Max(maxA, projection);
                }

                // 计算另一个图形在轴上的投影
                float minB = float.MaxValue;
                float maxB = float.MinValue;
                foreach (Vector2 vertex in other.GetWorldVertices()) {
                    float projection = Vector2.Dot(vertex, axis);
                    minB = Math.Min(minB, projection);
                    maxB = Math.Max(maxB, projection);
                }

                // 检查投影是否重叠
                if (maxA < minB || maxB < minA) {
                    // 发现分离轴，没有碰撞
                    return false;
                }
            }

            // 所有轴上都发现重叠，存在碰撞
            return true;
        }

        /// <summary>
        /// 绘制几何图形（用于调试）
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch实例</param>
        /// <param name="texture">用于绘制点的纹理</param>
        /// <param name="color">绘制颜色</param>
        public void Draw(SpriteBatch spriteBatch, Texture2D texture, Color color) {
            List<Vector2> worldVertices = GetWorldVertices();

            // 绘制顶点
            foreach (Vector2 vertex in worldVertices) {
                spriteBatch.Draw(texture, vertex, color);
            }

            // 绘制边
            for (int i = 0; i < worldVertices.Count; i++) {
                Vector2 start = worldVertices[i];
                Vector2 end = worldVertices[(i + 1) % worldVertices.Count];

                // 计算边的角度和长度
                float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
                float length = Vector2.Distance(start, end);

                // 绘制边
                spriteBatch.Draw(texture,
                                start,
                                null,
                                color,
                                angle,
                                Vector2.Zero,
                                new Vector2(length, 1),
                                SpriteEffects.None,
                                0);
            }
        }
    }
}