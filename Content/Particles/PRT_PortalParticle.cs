using CalamityEntropy.Assets.Register;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //门户低频特效,没开CanPool
    //frame在字段声明处Main.rand,池化复用不会重掷,全开CanPool门户全长同一帧,懒得折腾
    public class PRT_PortalParticle : BasePRT
    {
        public bool Glow = true;
        public int frame = Main.rand.Next(0, 16);
        public int FadingTime = 10;

        //真图是DrawVortex里的Voronoi,Texture指白图只是堵HasAsset的Warn
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_PortalParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 30;
        }

        public override bool PreDraw(SpriteBatch sb) {
            float alpha = Lifetime - Time > FadingTime ? 1 : ((Lifetime - Time) / (float)FadingTime);
            //两圈顺序固定:先外圈原色再内圈0.6白芯,反了白芯会被外圈吞
            //每圈各End/Begin+完整Vortex参数,不是一层shader画两圈
            DrawVortex(sb, Position, Color * alpha, Scale);
            DrawVortex(sb, Position, Color.White * alpha, Scale * 0.6f);
            return false;
        }

        public void DrawVortex(SpriteBatch sb, Vector2 pos, Color color, float Size = 1, float glow = 1f) {
            //Vortex.fx固定Additive+ZoomMatrix,跟PRT桶Blend对不上,每圈End/Begin一次
            sb.End();
            Effect effect = CEEffectAssets.Vortex;
            //Vortex.fx:Center漩涡中心UV,Strength扭曲强度,AspectRatio压扁比
            //TexOffset跟GlobalTimeWrappedHourly走让Voronoi流动,FadeOutDistance/Width管边缘柔化
            //enhanceLightAlpha抬中心亮度,跟后面DrawGlow叠一起才是旧门户观感
            effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
            effect.Parameters["Strength"].SetValue(22);
            effect.Parameters["AspectRatio"].SetValue(1);
            effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
            float fadeOutDistance = 0.06f;
            float fadeOutWidth = 0.3f;
            effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
            effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
            effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Texture2D voronoi = PRTExtraTextures.VoronoiShapes.Value;
            sb.Draw(voronoi, pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, voronoi.Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
            //每圈内第二趟DrawGlow,Additive柔光叠在Voronoi上面;它内部也End批次,所以后面还得再BeginDrawingWithMode
            CEUtils.DrawGlow(pos, Color.White * 0.4f * glow, 0.8f * Size * glow);
            sb.End();
            //DrawGlow内部也会动批次,End完BeginDrawingWithMode还回去
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
        }
    }


}
