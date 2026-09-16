using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_TrailParticle : BasePRT
    {
        public bool Glow = true;
        //odp是轨迹点List,池化复用忘Clear就是上一条轨迹凭空闪现,这类干脆不开CanPool
        public List<Vector2> odp = new List<Vector2>();
        public bool SameAlpha = false;
        public bool ExtraLight = true;
        public bool ShouldDraw = true;
        public int maxLength = 64;

        public override string Texture => "CalamityEntropy/Content/Particles/Trail";

        public PRT_TrailParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            //Lifetime 13是旧TrailParticle默认,到点Kill是框架的事,这里只管兜底
            if (Lifetime <= 0)
                Lifetime = 13;
        }

        public void AddPoint(Vector2 pos) {
            //没AI采样,调用点每帧推Position;Insert(0)新点在头,跟旧drawAll外部AddPoint一致
            odp.Insert(0, pos);
            if (odp.Count > maxLength)
                odp.RemoveAt(odp.Count - 1);
        }

        public void DrawTrail(SpriteBatch sb) {
            if (odp.Count < 3)
                return;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            //进来时PRT批次是开着的,图元Immediate+LinearWrap得自己Begin
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D tex = PRTSharedAssets.Trail.Value;
            //fade用Lifetime-Time/12f不是LifetimeCompletion,旧drawAll就这么写的,别统一成新进度
            float remaining = Lifetime - Time;
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color * (remaining / 12f);
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 12 * Scale,
                    new Vector3(0f / odp.Count, 1, 1), b));
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 12 * Scale,
                    new Vector3(0f / odp.Count, 0, 1), b));
                for (int i = 1; i < odp.Count; i++) {
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 12 * Scale,
                        new Vector3((i / (odp.Count - 1f)) * 0.998f, 1, 1), b * (SameAlpha ? 1 : ((odp.Count - i - 1) / (float)odp.Count))));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 12 * Scale,
                        new Vector3((i / (odp.Count - 1f)) * 0.998f, 0, 1), b * (SameAlpha ? 1 : ((odp.Count - i - 1) / (float)odp.Count))));
                }
                if (ve.Count >= 3) {
                    gd.Textures[0] = tex;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            if (ExtraLight) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.White * (remaining / 12f);
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 12 * Scale,
                    new Vector3(0f / odp.Count, 1, 1), b));
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 12 * Scale,
                    new Vector3(0f / odp.Count, 0, 1), b));
                for (int i = 1; i < odp.Count; i++) {
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * Scale,
                        new Vector3(i / (odp.Count - 1f), 1, 1), b * (SameAlpha ? 1 : ((odp.Count - i - 1) / (float)odp.Count))));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * Scale,
                        new Vector3(i / (odp.Count - 1f), 0, 1), b * (SameAlpha ? 1 : ((odp.Count - i - 1) / (float)odp.Count))));
                }
                if (ve.Count >= 3) {
                    gd.Textures[0] = tex;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            sb.End();
            //End完必须BeginDrawingWithMode接回去,这里固定Deferred跟旧drawAll一致
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb, SpriteSortMode.Deferred);
        }

        public override bool PreDraw(SpriteBatch sb) {
            if (!ShouldDraw)
                return false;
            DrawTrail(sb);
            return false;   //不走基类Draw,批次全在DrawTrail里自己管
        }
    }



    public class PRT_TrailGunShot : BasePRT
    {
        public bool Glow = true;
        public int trailLength = 6;
        //轨迹List同上,不池化
        public List<Vector2> trailPositions = new List<Vector2>();

        public override string Texture => "CalamityEntropy/Content/Particles/Trail";

        public PRT_TrailGunShot Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 200;
        }

        internal Color ColorFunction(float completionRatio, Vector2 vertex) {
            //PrimitiveRenderer的completion是0→1已过,旧色带要剩余比例,得1减一下
            completionRatio = 1 - completionRatio;
            float fadeOpacity = Math.Min((Lifetime - Time) / (float)trailLength, 1f);
            return Color.PaleGoldenrod * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio, Vector2 vertex) {
            float width = completionRatio * 8f;
            return width > 0 ? width : 0;
        }

        public override void AI() {
            //尾追加序列,index 0是最老点,跟上面odp Insert(0)那套方向相反,别混用
            trailPositions.Add(Position);
            if (trailPositions.Count > trailLength)
                trailPositions.RemoveAt(0);
        }

        public override bool PreDraw(SpriteBatch sb) {
            //原先走灾厄PrimitiveRenderer+TrailStreak shader,脱离灾厄后换成自有BasicTrail贴图的三角带,同为渐隐流光
            if (trailPositions is null || trailPositions.Count < 3)
                return false;

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D tex = PRTSharedAssets.BasicTrail.Value;
            //旧RenderTrail的offset函数是每点加Vector2.One*Scale*0.5f,保持这个常量位移
            Vector2 posOffset = Vector2.One * Scale * 0.5f;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            for (int i = 1; i < trailPositions.Count; i++) {
                float c = i / (trailPositions.Count - 1f);
                float width = WidthFunction(c, Vector2.Zero);
                Color col = ColorFunction(c, Vector2.Zero);
                Vector2 normal = (trailPositions[i] - trailPositions[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.PiOver2);
                Vector2 basePos = trailPositions[i] + posOffset - Main.screenPosition;
                ve.Add(new ColoredVertex(basePos + normal * width, new Vector3(c, 1, 1), col));
                ve.Add(new ColoredVertex(basePos - normal * width, new Vector3(c, 0, 1), col));
            }
            if (ve.Count >= 3) {
                gd.Textures[0] = tex;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            //End完接回PRT批次,别让后面同桶粒子吃到Immediate状态
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
            return false;
        }
    }


}
