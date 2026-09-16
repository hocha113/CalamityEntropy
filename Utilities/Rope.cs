using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Utilities
{
    public class Rope
    {
        public Vector2 Start { get; set; }

        public Vector2 End { get; set; }

        public Rope(Vector2 startPos, int segmentCount, float segmentLength, Vector2 gravity, float damping = 0f, int accuracy = 15, bool tileCollide = false) {
            this.segments = new List<Rope.RopeSegment>();
            for (int i = 0; i < segmentCount; i++) {
                this.segments.Add(new Rope.RopeSegment(startPos + Utils.SafeNormalize(gravity, Vector2.Zero) * (float)i));
            }
            this.Start = startPos;
            this.End = startPos;
            this.segmentLength = segmentLength;
            this.gravity = gravity;
            this.damping = damping;
            this.accuracy = Math.Max(1, accuracy);
            this.twoPoint = false;
            this.tileCollide = tileCollide;
        }

        public Rope(Vector2 startPoint, Vector2 endPoint, int segmentCount, float segmentLength, Vector2 gravity, float damping = 0f, int accuracy = 15, bool tileCollide = false) {
            this.segments = new List<Rope.RopeSegment>();
            for (int i = 0; i < segmentCount; i++) {
                this.segments.Add(new Rope.RopeSegment(Vector2.Lerp(startPoint, endPoint, (float)i / (float)(segmentCount - 1))));
            }
            this.Start = startPoint;
            this.End = endPoint;
            this.segmentLength = segmentLength;
            this.gravity = gravity;
            this.damping = damping;
            this.accuracy = Math.Max(1, accuracy);
            this.twoPoint = true;
            this.tileCollide = tileCollide;
        }

        public List<Vector2> GetPoints() {
            List<Vector2> points = new List<Vector2>();
            foreach (Rope.RopeSegment segment in this.segments) {
                points.Add(segment.position);
            }
            return points;
        }

        public void Update() {
            this.segments[0].position = this.Start;
            bool flag = this.twoPoint;
            if (flag) {
                this.segments[this.segments.Count - 1].position = this.End;
            }
            for (int i = 0; i < this.segments.Count; i++) {
                bool flag2 = Utils.HasNaNs(this.segments[i].position);
                if (flag2) {
                    this.segments[i].position = this.segments[0].position;
                }
                Vector2 velocity = (this.segments[i].position - this.segments[i].oldPosition) / (1f + this.damping) + this.gravity + this.segments[i].velocity;
                velocity = this.TileCollision(this.segments[i].position, velocity);
                this.segments[i].oldPosition = this.segments[i].position;
                this.segments[i].position += velocity;
            }
            for (int j = 0; j < this.accuracy; j++) {
                this.ConstrainPoints();
            }
        }

        private void ConstrainPoints() {
            for (int i = 0; i < this.segments.Count - 1; i++) {
                float dist = (this.segments[i].position - this.segments[i + 1].position).Length();
                float error = MathF.Abs(dist - this.segmentLength);
                Vector2 changeDirection = Vector2.Zero;
                bool flag = dist > this.segmentLength;
                if (flag) {
                    changeDirection = Utils.SafeNormalize(this.segments[i].position - this.segments[i + 1].position, Vector2.Zero);
                }
                else {
                    bool flag2 = dist < this.segmentLength;
                    if (flag2) {
                        changeDirection = Utils.SafeNormalize(this.segments[i + 1].position - this.segments[i].position, Vector2.Zero);
                    }
                }
                Vector2 changeAmount = changeDirection * error;
                bool flag3 = i != 0;
                if (flag3) {
                    this.segments[i].position += this.TileCollision(this.segments[i].position, changeAmount * -0.5f);
                    this.segments[i + 1].position += this.TileCollision(this.segments[i + 1].position, changeAmount * 0.5f);
                }
                else {
                    this.segments[i + 1].position += this.TileCollision(this.segments[i + 1].position, changeAmount);
                }
            }
            bool flag4 = !this.twoPoint;
            if (flag4) {
                this.End = this.segments[this.segments.Count - 1].position;
            }
        }

        private Vector2 TileCollision(Vector2 position, Vector2 velocity) {
            bool flag = !this.tileCollide;
            Vector2 result;
            if (flag) {
                result = velocity;
            }
            else {
                Vector2 newVelocity = Collision.noSlopeCollision(position - new Vector2(3f), velocity, 6, 6, true, true);
                Vector2 final = velocity;
                bool flag2 = Math.Abs(newVelocity.X) < Math.Abs(velocity.X);
                if (flag2) {
                    final.X = 0f;
                }
                bool flag3 = Math.Abs(newVelocity.Y) < Math.Abs(velocity.Y);
                if (flag3) {
                    final.Y = 0f;
                }
                result = final;
            }
            return result;
        }

        public List<Rope.RopeSegment> segments;

        public int accuracy;

        public float segmentLength;

        public Vector2 gravity;

        public bool twoPoint;

        public float damping;

        public bool tileCollide;

        public class RopeSegment
        {
            public RopeSegment(Vector2 pos) {
                this.position = pos;
                this.velocity = Vector2.Zero;
                this.oldPosition = pos;
            }

            public Vector2 position;

            public Vector2 velocity;

            public Vector2 oldPosition;
        }
    }
}
