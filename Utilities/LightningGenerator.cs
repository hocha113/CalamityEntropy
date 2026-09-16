using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Utilities
{
    public class LightningGenerator
    {

        public static List<Vector2> GenerateLightning(Vector2 start, Vector2 end, float displacement, int detailLevel) {
            var points = new List<Vector2> { start, end };
            Subdivide(points, displacement, detailLevel);
            return points;
        }

        private static void Subdivide(List<Vector2> points, float displacement, int depth) {
            var random = Main.rand;
            if (depth <= 0 || displacement <= 0)
                return;

            for (int i = points.Count - 1; i > 0; i--) {
                var start = points[i - 1];
                var end = points[i];

                var mid = (start + end) / 2;

                var offsetX = (float)(random.NextDouble() * 2 - 1) * displacement;
                var offsetY = (float)(random.NextDouble() * 2 - 1) * displacement;
                var offset = new Vector2(offsetX, offsetY);

                var midPoint = mid + offset;

                points.Insert(i, midPoint);
            }

            Subdivide(points, displacement / 2, depth - 1);
        }
    }

    public class LightningAdvanced
    {
        public Vector2 Point1;
        public Vector2 Point2;
        public int SegCount;
        public float GravityMin;
        public float GravityMax;
        public Rope rope;
        public LightningAdvanced(Vector2 point1, Vector2 point2, int segs = 40, float gravMin = 0.3f, float gravMax = 0.6f) {
            this.Point1 = point1;
            this.Point2 = point2;
            this.SegCount = segs;
            this.GravityMin = gravMin;
            this.GravityMax = gravMax;
            rope = new Rope(point1, point2, SegCount, CEUtils.getDistance(point1, point2) / (float)SegCount, randomGrav(), 0, 15, false);
        }
        public void Update() {
            this.rope.segmentLength = CEUtils.getDistance(Point1, Point2) / (float)SegCount;
            this.rope.gravity = Vector2.Lerp(this.rope.gravity, Vector2.Zero, 0.1f);
            this.rope.Update();
            if (Main.rand.NextBool(40)) {
                rope = new Rope(Point1, Point2, SegCount, CEUtils.getDistance(Point1, Point2) / (float)SegCount, randomGrav(), 0, 15, false);
            }
        }
        public void Update(Vector2 p1, Vector2 p2) {
            Point1 = p1;
            Point2 = p2;
            this.rope.Start = p1;
            this.rope.End = p2;
            Update();
        }
        public Vector2 randomGrav() {
            return CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(this.GravityMin, this.GravityMax);
        }
        public List<Vector2> GetPoints() {
            return this.rope.GetPoints();
        }
    }
}
