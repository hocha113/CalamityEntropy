using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //弹壳粒子:重力下坠+横速衰减+落地前渐隐,机枪类武器抛壳用
    public class PRT_ShellParticle : BasePRT
    {
        public bool Glow = true;
        public int Fade = 20;
        public float Gravity = 0.56f;

        public override string Texture => "CalamityEntropy/Content/Particles/Shell";

        public PRT_ShellParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 70;   //旧ShellParticle的OnSpawn默认
        }

        public override void AI() {
            //Position+=Velocity框架做
            Velocity.Y += Gravity;
            Rotation += Velocity.X * 0.05f;
            Velocity.X *= 0.96f;
            //旧Lifetime(剩余tick)<Fade开始渐隐,PRT里剩余=Lifetime-Time
            if (Lifetime - Time < Fade)
                Opacity -= 1f / Fade;
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
