using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_MCodeParticle : BasePRT
    {
        public int frame = Main.rand.Next(0, 16);   //没开CanPool,声明处掷随机没事

        //t0~t15共16帧,Texture挂t0,真图PreDraw里PRTFrameTextures.MCode(frame)
        public override string Texture => "CalamityEntropy/Assets/Extra/MALICIOUSCODE/t0";

        public PRT_MCodeParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 60;
        }

        public override void AI() {
            if (Main.rand.NextBool(28)) {
                if (Main.rand.NextBool())
                    frame = Main.rand.Next(0, 16);
                else
                    Position += CEUtils.randomVec(16);
            }
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTFrameTextures.MCode(frame);
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }

        public bool Glow = true;
    }


}
