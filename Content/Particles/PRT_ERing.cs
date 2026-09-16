using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_ERing : BasePRT
    {
        public bool Glow = true;
        public int PointCount = 64;
        public int LineWidth = 4;
        private float distance = 0;

        //不画这张白图,环是PreDraw里TriangleStrip+MegaStreakBacking2,Texture只是塞HasAsset
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_ERing Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 200;   //distance从0帧起外扩,字段默认0不用Reset
        }

        public override void AI() {
            //环带半径=Scale*已过寿命比,内径max(0,distance-LineWidth/2)
            distance = Scale * LifetimeCompletion;
        }

        public (List<Vector2>, List<Vector2>) calPoints() {
            //内外两圈各PointCount+1点,交错喂TriangleStrip拼空心环
            List<Vector2> inside = new List<Vector2>();
            List<Vector2> outside = new List<Vector2>();

            float rAdd = MathHelper.TwoPi / PointCount;
            for (int i = 0; i <= PointCount; i++) {
                float rot = rAdd * i;
                float inA = distance - LineWidth / 2f;
                float outA = distance + LineWidth / 2f;

                if (inA < 0)
                    inA = 0;
                inside.Add(rot.ToRotationVector2() * inA);
                outside.Add(rot.ToRotationVector2() * outA);
            }
            return (inside, outside);
        }

        public override bool PreDraw(SpriteBatch sb) {
            var points = calPoints();
            var pIn = points.Item1;
            var pOut = points.Item2;
            float alpha = 1f - LifetimeCompletion;   //淡出跟旧剩余比例走
            List<ColoredVertex> vertices = new List<ColoredVertex>();
            for (int i = 0; i < pIn.Count; i++) {
                Color c = Color * alpha * Opacity;
                //顶点序:in0,out0,in1,out1…TriangleStrip每多一对顶点就多2个三角
                //texcoord.x=i/(Count-1)沿环扫MegaStreakBacking2,内圈y=1外圈y=0
                vertices.Add(new ColoredVertex(Position + pIn[i] - Main.screenPosition, new Vector3((float)i / (pIn.Count - 1), 1, 1), c));
                vertices.Add(new ColoredVertex(Position + pOut[i] - Main.screenPosition, new Vector3((float)i / (pIn.Count - 1), 0, 1), c));
            }
            //进来时PRT批次是开着的,Immediate+Additive画图元得先End
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (vertices.Count >= 3) {
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = PRTExtraTextures.MegaStreakBacking2.Value;
                //primitiveCount是vertices.Count-2不是Count/2,传错只画半截环
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);
            }
            sb.End();
            //End完必须BeginDrawingWithMode接回去,少一步同桶后面全花
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
            return false;
        }
    }


}
