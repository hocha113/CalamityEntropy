using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_WindParticle : BasePRT
    {
        //rv/r/dir在字段初始化器里掷随机,池化不会重跑,这类带轨迹List干脆不池化
        public List<Vector2> odp = new List<Vector2>();
        public float v1 = 12;
        public float v2 = 3;
        public float rv = Main.rand.NextFloat(0.14f, 0.2f);
        public float r = CEUtils.randomRot();
        public int dir = Main.rand.NextBool() ? 1 : -1;
        public bool Glow = true;

        public override string Texture => "CalamityEntropy/Content/Particles/Wind";

        public PRT_WindParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            //Lifetime 46旧Wind默认,-1漏设就永生一路囤odp
            if (Lifetime <= 0)
                Lifetime = 46;
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //衰减放AI不是PreDraw,跟旧updateAll一致
            Velocity = Rotation.ToRotationVector2() * v1 + r.ToRotationVector2() * v2;
            Rotation += dir * rv;
            //每帧Insert(0)采样,最多16点;旧WindParticle原值
            odp.Insert(0, Position);
            if (odp.Count > 16)
                odp.RemoveAt(odp.Count - 1);
        }

        public Color TrailColor(float completionRatio, Vector2 vertex) {
            return Color * completionRatio * Opacity * new Vector2(1, 0).RotatedBy(completionRatio * MathHelper.Pi).Y;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return Scale * 26;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //原先走灾厄PrimitiveRenderer+ArtAttack shader,脱离灾厄后换成自有Wind贴图的三角带
            //宽度/颜色带保持原TrailWidth/TrailColor曲线,风痕淡入淡出形状不变
            if (odp.Count < 3)
                return false;

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D tex = PRTSharedAssets.Wind.Value;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            for (int i = 1; i < odp.Count; i++) {
                float c = i / (odp.Count - 1f);
                float halfWidth = TrailWidth(c, Vector2.Zero) * 0.5f;
                Color col = TrailColor(c, Vector2.Zero);
                Vector2 normal = (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.PiOver2);
                Vector2 basePos = odp[i] - Main.screenPosition;
                ve.Add(new ColoredVertex(basePos + normal * halfWidth, new Vector3(c, 1, 1), col));
                ve.Add(new ColoredVertex(basePos - normal * halfWidth, new Vector3(c, 0, 1), col));
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
