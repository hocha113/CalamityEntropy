using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //雪花粒子:末6tick渐出+随横速自旋+速度衰减
    public class PRT_Snowflake : BasePRT
    {
        public bool Glow = true;

        public override string Texture => "CalamityEntropy/Content/Particles/Snowflake";

        public PRT_Snowflake Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 36;   //旧Snowflake的OnSpawn默认
        }

        public override void AI() {
            //旧Opacity=min(Lifetime剩余/6,1):只在最后6tick渐出
            Opacity = float.Min((Lifetime - Time) / 6f, 1f);
            Rotation += Velocity.X * 0.01f;
            Velocity *= 0.99f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //旧EParticle基类默认Draw的逐行等价
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
