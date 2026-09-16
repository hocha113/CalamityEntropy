using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_TrailSparkParticle : BasePRT
    {
        public bool Glow = true;
        public List<Vector2> odp = new List<Vector2>();
        public int maxLength = 7;
        public float gravity = 0.9f;
        public float gA = 0;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            odp.Clear();   //轨迹点List,池化忘Clear视觉bug极难查
            maxLength = 7;
            gravity = 0.9f;
            gA = 0f;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/Trail";

        public PRT_TrailSparkParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 30;   //到点Kill交给框架,AI不管自杀
        }

        public override void AI() {
            //无条件每帧AddPoint,没有addPoint开关那套
            AddPoint(Position);
            Velocity += gravity * Vector2.UnitY * gA;
            //gA 0→1渐增,旧TrailSpark重力是慢慢压下来的
            if (gA < 1)
                gA += 0.025f;
        }

        public void AddPoint(Vector2 pos) {
            odp.Insert(0, pos);
            if (odp.Count > maxLength)
                odp.RemoveAt(odp.Count - 1);
        }

        public override bool PreDraw(SpriteBatch sb) {
            //odp<3在这return还没动SpriteBatch,别照搬Antivoid/DashBeam那套早退End
            if (odp.Count < 3)
                return false;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);   //图元Immediate+LinearWrap,跟PRT Deferred不兼容得自己开

            float remaining = 1f - LifetimeCompletion;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = Color * (remaining * Lifetime / 8f);
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 4 * Scale,
                new Vector3(0f / odp.Count, 1, 1), b));
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 4 * Scale,
                new Vector3(0f / odp.Count, 0, 1), b));
            for (int i = 1; i < odp.Count; i++) {
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 4 * Scale,
                    new Vector3((float)i / odp.Count, 1, 1), b * ((odp.Count - i) / (float)odp.Count)));
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 4 * Scale,
                    new Vector3((float)i / odp.Count, 0, 1), b * ((odp.Count - i) / (float)odp.Count)));
            }
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3) {
                gd.Textures[0] = PRTLoader.PRT_IDToTexture[ID];
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);   //画完还批次,删这两行同桶后面全炸
            return false;
        }
    }


}
