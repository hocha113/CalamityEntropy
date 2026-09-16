using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_DashBeam : BasePRT
    {
        public bool Glow = true;
        //DashBeam轨迹最长120点,池化忘Clear很离谱,不开CanPool
        public List<Vector2> odp = new List<Vector2>();
        public int maxLength = 120;
        public bool addPoint = false;   //dash段才开,静止帧不采样免得odp挤一坨
        public float gravity = 0;
        public float gA = 1;

        public override string Texture => "CalamityEntropy/Content/Particles/DashBeam";

        public PRT_DashBeam Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 30;   //旧DashBeam默认,漏设-1永生120点轨迹堆满很离谱
        }

        public override void AI() {
            //addPoint同Antivoid,冲刺光束采样权在调用点不在AI
            if (addPoint)
                AddPoint(Position);
            //fade用(Lifetime-Time)/30f,跟Antivoid的1-LifetimeCompletion写法不同但是旧值
            Color.A = (byte)(255 * ((Lifetime - Time) / 30f));
        }

        public void AddPoint(Vector2 pos) {
            odp.Insert(0, pos);
            if (odp.Count > maxLength)
                odp.RemoveAt(odp.Count - 1);
        }

        public override bool PreDraw(SpriteBatch sb) {
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);   //光束贴图沿轨迹UV,LinearWrap
            if (odp.Count < 3) {
                sb.End();
                PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);   //早退也得还批次
                return false;
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = Color * 0.4f;
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 60 * Scale,
                      new Vector3((((float)0) / odp.Count), 1, 1),
                      b));
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 60 * Scale,
                  new Vector3((((float)0) / odp.Count), 0, 1),
                  b));
            for (int i = 1; i < odp.Count; i++) {
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 60 * Scale,
                      new Vector3((((float)i) / odp.Count), 1, 1),
                      b));
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 60 * Scale,
                      new Vector3((((float)i) / odp.Count), 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = PRTSharedAssets.DashBeam.Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);   //Immediate画完接回PRT Deferred
            return false;
        }
    }


}
