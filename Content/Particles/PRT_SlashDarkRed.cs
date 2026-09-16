using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //旧drawAll先全体Draw再全体DrawEffect,迁成PreDraw+PostDraw;斩击通常单发层次差可接受
    //二次绘制对批次顺序敏感,没开CanPool
    //同类多实例叠一起时层次跟旧版不完全一样(旧=全主体后全内芯,新=逐粒子主体+内芯),单发斩击无所谓
    public class PRT_SlashDarkRed : BasePRT
    {
        public bool Glow = true;
        public float sW = 1;
        public float height = 0.4f;
        public Color colorInside = Color.Black;
        public float scw = 0.36f;

        public override string Texture => "CalamityEntropy/Content/Particles/Sn2";

        public PRT_SlashDarkRed Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 16;
        }

        public override void AI() {
            float remaining = 1f - LifetimeCompletion;
            sW = float.Lerp(sW, 0, remaining * 0.1f);
            Velocity *= 0.9f;
            Opacity = remaining / (1f / 1.8f);
            if (Opacity > 1)
                Opacity = 1;
        }

        public void DrawSlash(float width, float HeightMult, float scale, float rotation, Color color, Vector2 center, SpriteBatch sb) {
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<Vector2> p1 = new List<Vector2>();
            List<Vector2> p2 = new List<Vector2>();
            for (float i = -MathHelper.PiOver2; i <= MathHelper.PiOver2; i += MathHelper.Pi / 26f) {
                p2.Add(((i * 1f).ToRotationVector2() * width * new Vector2(1, HeightMult)).RotatedBy(rotation));
                p1.Add(((i * 1f).ToRotationVector2() * width * new Vector2(1 - scale, HeightMult)).RotatedBy(rotation));
            }
            //内外弧成对顶点+TriangleStrip拉月牙,贴图是白图——颜色全靠顶点Color传进去
            for (int i = 0; i < p1.Count; i++) {
                Color b = color;
                ve.Add(new ColoredVertex(center - Main.screenPosition + p1[i], new Vector3(i / ((float)p1.Count - 1), 1, 1), b));
                ve.Add(new ColoredVertex(center - Main.screenPosition + p2[i], new Vector3(i / ((float)p1.Count - 1), 0, 1), b));
            }
            if (ve.Count >= 3) {
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = PRTExtraTextures.White.Value;
                //没shader,Immediate+白图+图元,primitive数照旧是ve.Count-2
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
        }

        public override bool PreDraw(SpriteBatch sb) {
            //第一遍PreDraw:8个45°扇形铺外圈,DrawSlash里TriangleStrip+白图
            //必须先End PRT批次再Immediate,图元不吃Deferred里的排队
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, PRTRender.GetBlendStateFor(PRTDrawMode), SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            float scale = sW;
            for (float r = 0; r < 359; r += 45) {
                float rot = r.ToRadians();
                DrawSlash(Scale * 128, height, scale * scw, Rotation, Color * Opacity, Position + r.ToRotationVector2() * 2, sb);
            }
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
            return false;
        }

        public override void PostDraw(SpriteBatch sb) {
            //第二遍PostDraw:旧DrawEffect,colorInside黑色内芯叠在PreDraw外圈上面
            //两趟顺序反了内芯会被八瓣盖死;池化复用若带着脏sW/scw也会炸,所以不开CanPool
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, PRTRender.GetBlendStateFor(PRTDrawMode), SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawSlash(Scale * 128, height, sW * scw, Rotation, colorInside * Opacity, Position, sb);
            sb.End();
            //PostDraw自己End/Begin过,必须BeginDrawingWithMode接回PRT桶
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
        }
    }


}
