using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //EParticle迁来的MediumSmoke,Azafure重盔喷气trail用这粒,跟MediumMistCal不是一回事
    public class PRT_EMediumSmoke : BasePRT
    {
        public Color orgColor;
        public float rotDir = 1;
        public int texT;
        public float wl = 1;

        public override bool CanPool => true;

        public override void Reset() {
            //CanPool,texT/rotDir在SetProperty里重掷,Reset漏了复用粒子全长同一帧
            base.Reset();
            orgColor = default;
            rotDir = 1;
            texT = 0;
            wl = 1;
            Glow = true;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/MediumSmoke";

        public PRT_EMediumSmoke Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 80;
            orgColor = Color;
            rotDir = Main.rand.NextBool() ? 1 : -1;
            texT = Main.rand.Next(0, 3);
            wl = 1;
        }

        public override void AI() {
            if (wl > 0)
                wl -= 0.1f / (80f / Lifetime);   //wl白化插值,寿命越长衰减越慢,老代码原公式
            Rotation += rotDir * 0.03f;
            Opacity = 1f - LifetimeCompletion;   //旧alpha走remaining比例,等价写法
            Color = Color.Lerp(Color, new Color(30, 40, 30), 0.03f / (80f / Lifetime));
            Velocity *= 0.94f;
            Velocity += new Vector2(0, -0.1f);   //微上浮,框架还会Position+=Velocity,别在AI里再写一遍
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            clr = Color.Lerp(clr, Color.White, wl);
            sb.Draw(tex, Position - Main.screenPosition, CEUtils.GetCutTexRect(tex, 3, texT, false), clr * Opacity, Rotation, tex.Size() / 2f * new Vector2(1, 0.3333f), Scale, SpriteEffects.None, 0);   //3列横排切帧,texT随机0-2
            return false;
        }

        public bool Glow = true;
    }


}
