using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_AntivoidTrail : BasePRT
    {
        public bool Glow = true;
        //轨迹List,不开CanPool,忘Clear就是上一条反虚空拖影还在
        public List<Vector2> odp = new List<Vector2>();
        public int maxLength = 60;
        public bool addPoint = false;
        //gravity/gA旧字段还在,这版AI没接重力,别看见以为是漏写
        public float gravity = 0;
        public float gA = 1;

        public override string Texture => "CalamityEntropy/Content/Particles/AntivoidTrail";

        public PRT_AntivoidTrail Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            //Lifetime 30旧AntivoidTrail默认,-1漏设就永生,odp会一直囤到maxLength
            if (Lifetime <= 0)
                Lifetime = 30;
        }

        public void AddPoint(Vector2 pos) {
            odp.Insert(0, pos);
            if (odp.Count > maxLength)
                odp.RemoveAt(odp.Count - 1);
        }

        public override void AI() {
            //addPoint默认false,采样节奏在调用点;不是每帧无脑拖尾
            if (addPoint)
                AddPoint(Position);
            //没显式Kill,alpha随寿命淡出;到点自杀还是框架Lifetime计数
            Color.A = (byte)(255 * (1f - LifetimeCompletion));   //旧剩余比例1→0,得用1-LifetimeCompletion
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTSharedAssets.AntivoidTrail.Value;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);   //轨迹UV沿长度铺,LinearWrap
            if (odp.Count < 3) {
                sb.End();
                PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);   //点不够也得还批次,少这一步同桶后面全遭殃
                return false;
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = Color;
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 20 * Scale,
                new Vector3(0f / odp.Count, 1, 1), b));
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 20 * Scale,
                new Vector3(0f / odp.Count, 0, 1), b));
            for (int i = 1; i < odp.Count; i++) {
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 20 * Scale,
                    new Vector3(i / (float)odp.Count, 1, 1), b));
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 20 * Scale,
                    new Vector3(i / (float)odp.Count, 0, 1), b));
            }
            if (ve.Count >= 3) {
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = tex;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);   //Immediate批次画完,接回PRT Deferred
            return false;
        }
    }


}
