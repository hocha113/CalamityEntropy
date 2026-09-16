using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{


    public class PRT_Sn : BasePRT
    {
        public bool Glow = true;

        public override bool CanPool => true;

        //CanPool复用,Glow以外新加字段记得来Reset
        public override void Reset() {
            base.Reset();
            Glow = true;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/Sn";

        public PRT_Sn Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            Opacity = 1f - LifetimeCompletion;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            //NonPremultiplied只乘A,Additive/AlphaBlend走clr*=Opacity
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
