using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{


    //Echo(BNE)涟漪,贴图借HadCircle,短命缩放淡出
    public class PRT_EchoCircle : BasePRT
    {
        public bool Glow = true;

        public override string Texture => "CalamityEntropy/Content/Particles/HadCircle";

        public PRT_EchoCircle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 6;   //旧默认6,很短
        }

        public override void AI() {
            float remaining = 1f - LifetimeCompletion;   //旧剩余比例,别直接拿LifetimeCompletion当alpha
            Opacity = remaining;
            Scale = remaining * 0.22f;
            Velocity *= 0.96f;   //边扩边减速,框架还会再Position+=Velocity
        }

        public override bool PreDraw(SpriteBatch sb) {
            //跟HadLine/HadCircle同一套Glow/NonPremultiplied分支
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation,
                tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
