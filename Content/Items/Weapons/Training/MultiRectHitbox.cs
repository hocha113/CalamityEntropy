using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Items.Weapons.Training
{
    public class MultiRectHitbox
    {
        List<Rectf> hitboxs;
        public MultiRectHitbox() {
            hitboxs = new List<Rectf>();
        }
        public MultiRectHitbox(List<Rectf> rects) {
            hitboxs = rects;
        }
        public MultiRectHitbox(Rectf rect) {
            hitboxs = new List<Rectf>() { rect };
        }
        public bool Colliding(Vector2 position, Rectangle rect, float rotation = 0) {
            foreach (Rectf rectf in hitboxs)
                if (rectf.GetAdjustedRectf(position, rotation).Colliding(rect, Vector2.Zero))
                    return true;
            return false;
        }
        public bool CheckCollidingBetter(Vector2 position, Rectangle rect, float rotation = 0, float nowFrame = -1, bool flip = false, float scale = 1) {
            foreach (Rectf rect_ in hitboxs) {
                var rectf = new Rectf(rect_.Center, rect_.Size);
                if (nowFrame != -1 && (nowFrame < rectf.ActiveFrame || (rectf.DeactiveFrame >= 0 && nowFrame > rectf.DeactiveFrame)))
                    continue;
                if (flip) {
                    rectf.Center = rectf.Center * new Vector2(-1, 1);
                }
                Vector2 ctr = rectf.Center * scale;
                rectf.Size *= scale;
                rectf.Center = ctr;

                var r = rectf.GetAdjustedRectf(position, rotation);
                Vector2 center = r.Center;
                Vector2 leftCenter = center - new Vector2(r.Width / 2f, 0).RotatedBy(rotation);
                Vector2 rightCenter = center + new Vector2(r.Width / 2f, 0).RotatedBy(rotation);
                if (CEUtils.LineThroughRect(leftCenter, rightCenter, rect, (int)r.Height))
                    return true;
            }
            return false;
        }
        public void Testing_DrawBox(Color color, float lineWidth, Vector2 position, float rotation = 0, bool flip = false, float scale = 1) {
            foreach (Rectf rect_ in hitboxs) {
                var rectf = new Rectf(rect_.Center, rect_.Size);
                if (flip) {
                    rectf.Center = rectf.Center * new Vector2(-1, 1);
                }
                Vector2 ctr = rectf.Center * scale;
                rectf.Size *= scale;
                rectf.Center = ctr;

                CEUtils.DrawRectAlt((rectf.GetAdjustedRectf(position, rotation)).ToRectangle(), color, lineWidth, 0);
            }
        }
    }
    public class Rectf
    {
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = Vector2.Zero;
        public Vector2 Center { get { return Position + Size / 2f; } set { Position = value - Size / 2f; } }
        public Rectf(float X, float Y, float width, float height) {
            Position = new Vector2(X, Y);
            Size = new Vector2(width, height);
        }
        public Rectf(Vector2 center, Vector2 size) {
            Position = center - size / 2f;
            Size = new Vector2(size.X, size.Y);
        }
        public Rectf() {
        }
        public int ActiveFrame = 0;
        public int DeactiveFrame = -1;
        public bool Colliding(Rectf other) {
            return this.ToRectangle().Intersects(other.ToRectangle());
        }
        public bool Colliding(Rectangle other) {
            return this.ToRectangle().Intersects(other);
        }
        public bool Colliding(Rectangle other, Vector2 offset) {
            return (this + offset).ToRectangle().Intersects(other);
        }
        public bool Colliding(Rectf other, Vector2 offset) {
            return (this + offset).ToRectangle().Intersects((other).ToRectangle());
        }
        public Rectf GetAdjustedRectf(Vector2 worldPos, float rotation) {
            var rectf = new Rectf();
            rectf.Width = this.Width;
            rectf.Height = this.Height;
            rectf.Center = worldPos + this.Center.RotatedBy(rotation);
            return rectf;
        }
        public static Rectf operator +(Rectf value1, Vector2 value2) {
            return new Rectf(value1.X + value2.X, value1.Y + value2.Y, value1.Width, value1.Height);
        }
        public static Rectf operator -(Rectf value1, Vector2 value2) {
            return new Rectf(value1.X - value2.X, value1.Y - value2.Y, value1.Width, value1.Height);
        }


        public float X { get { return Position.X; } set { Position.X = value; } }
        public float Y { get { return Position.Y; } set { Position.Y = value; } }
        public float Width { get { return Size.X; } set { Size.X = value; } }
        public float Height { get { return Size.Y; } set { Size.Y = value; } }

        public Rectangle ToRectangle() {
            return new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
        }
        public static Rectangle ToRectangle(float X, float Y, float Width, float Height) {
            return new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
        }
    }
}