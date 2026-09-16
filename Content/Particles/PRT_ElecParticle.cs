using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace CalamityEntropy.Content.Particles
{
    //IPixelPassPRT,绘制耦合EffectLoader两层门控:
    //1)CE_EffectHandler整段包在EnablePixelEffect里,关=config里像素特效全灭
    //2)PixelPass=true才进DrawPixelPassPRT→Screen2 RT→ApplyPixelShader; false时PreDraw也return false,旧行为就是不显示
    public class PRT_ElecParticle : BasePRT, IPixelPassPRT
    {
        public bool Glow = true;
        public bool PixelPass { get; set; }

        public override string Texture => "CalamityEntropy/Content/Particles/UpdraftParticle";

        public PRT_ElecParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //旧alpha是剩余比例,Completion 0→1,这样写方向对了
        }

        void DrawLines(SpriteBatch sb) {
            List<Vector2> lol = new List<Vector2>();
            for (int i = 0; i < 9; i++)
                lol.Add(Position + CEUtils.randomPointInCircle(32 * Scale));
            CEUtils.DrawLines(lol, Color * Opacity, 4);
        }

        public override bool PreDraw(SpriteBatch sb) {
            if (PixelPass)   //像素RT通道那边DrawPixelPass会画,这里跳过防双份
                return false;
            DrawLines(sb);
            return false;
        }

        public void DrawPixelPass(SpriteBatch sb) => DrawLines(sb);   //EffectLoader.DrawPixelPassPRT按PRTDrawMode分三桶后调这里
    }


}
