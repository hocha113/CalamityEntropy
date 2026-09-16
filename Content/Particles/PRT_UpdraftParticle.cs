using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //上升气流条,CanPool没开,blend走Configure尾参mode
    public class PRT_UpdraftParticle : BasePRT
    {
        public bool Glow = true;

        public override string Texture => "CalamityEntropy/Content/Particles/UpdraftParticle";

        public PRT_UpdraftParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 20;
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //1→0淡出
            Velocity *= 0.84f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            //NonPremultiplied只乘A,Additive/AlphaBlend走clr*=Opacity
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, new Vector2(200, 64), Scale, SpriteEffects.None, 0);
            return false;
        }
    }


}
