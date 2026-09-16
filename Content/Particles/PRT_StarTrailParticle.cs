using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_StarTrailParticle : BasePRT
    {
        public bool Glow = true;
        public List<Vector2> odp = new List<Vector2>();
        public int maxLength = 8;
        public bool addPoint = true;   //关采样就只画头辉不拖尾,调用点偶尔这么干
        public float gravity = 0;
        public float gA = 1;
        public int fadeOut = -1;   //-1按剩余比例整程渐隐,>0改成只在最后fadeOut tick内渐出

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            odp.Clear();   //池化复用,新加轨迹字段忘了Clear=下一个粒子拖着上一条星轨
            maxLength = 8;
            addPoint = true;
            gravity = 0f;
            gA = 1f;
            fadeOut = -1;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/StarTrail";

        public PRT_StarTrailParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            //出屏不杀跟旧EParticle一致,追踪类星轨出屏就断会很怪
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;   //旧StarTrail默认30
        }

        public void AddPoint(Vector2 pos) {
            odp.Insert(0, pos);
            if (odp.Count > maxLength)
                odp.RemoveAt(odp.Count - 1);
        }

        public override void AI() {
            //每帧Insert(0)采样+0.94衰减,轨迹族跟Antivoid/DashBeam同套路
            if (addPoint)
                AddPoint(Position);
            Velocity += gravity * Vector2.UnitY * gA;
            Rotation = Velocity.ToRotation();
            Velocity *= 0.94f;   //衰减系数旧值,改了星轨手感就飘了
        }

        public override bool PreDraw(SpriteBatch sb) {
            //fadeOut=-1整程按剩余比例渐隐;>0时只在最后fadeOut tick渐出,前段保持全亮
            //旧Lifetime(剩余tick)→PRT里是Lifetime-Time
            float fscale = fadeOut == -1 ? (1f - LifetimeCompletion) : float.Min(1f, (Lifetime - Time) / (float)fadeOut);
            Texture2D tex = PRTSharedAssets.StarTrail.Value;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, PRTDrawMode == PRTDrawModeEnum.AdditiveBlend ? BlendState.Additive : BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);   //Streak UV scroll靠LinearWrap
            sb.Draw(tex, Position - Main.screenPosition, null, Color * fscale, Rotation, tex.Size() / 2f, new Vector2(1.4f, 0.8f) * 0.22f * Scale, SpriteEffects.None, 0);
            sb.Draw(tex, Position - Main.screenPosition, null, Color * fscale * 1.2f, Rotation, tex.Size() / 2f, new Vector2(1.4f, 0.8f) * 0.22f * Scale * 0.55f, SpriteEffects.None, 0);
            if (odp.Count >= 3) {
                //双层带子:外层8px原色,内层3px提亮1.6x做芯,层内顶点结构相同只差宽度和亮度
                DrawTrailStrip(8f, Color * fscale);
                DrawTrailStrip(3f, Color * fscale * 1.6f);
            }
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);   //End完必须接回去,不然同桶后面粒子花屏
            return false;
        }

        private void DrawTrailStrip(float halfWidth, Color b) {
            List<ColoredVertex> ve = new List<ColoredVertex>();
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * halfWidth * Scale,
                new Vector3(-Main.GlobalTimeWrappedHourly * 2.5f, 1, 1), b));
            ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * halfWidth * Scale,
                new Vector3(-Main.GlobalTimeWrappedHourly * 2.5f, 0, 1), b));
            for (int i = 1; i < odp.Count; i++) {
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * halfWidth * Scale,
                    new Vector3((i / (float)odp.Count) - Main.GlobalTimeWrappedHourly * 2.5f, 1, 1), b * ((odp.Count - i) / (float)odp.Count)));
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * halfWidth * Scale,
                    new Vector3((i / (float)odp.Count) - Main.GlobalTimeWrappedHourly * 2.5f, 0, 1), b * ((odp.Count - i) / (float)odp.Count)));
            }
            if (ve.Count >= 3) {
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = PRTExtraTextures.Streak1w.Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
        }
    }


}
