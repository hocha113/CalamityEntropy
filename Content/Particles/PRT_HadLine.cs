using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_HadLine : BasePRT
    {
        public float hm = 1;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            hm = 1f;   //池化复用,调用点再改hm压扁
            Glow = true;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/HadLine";

        public PRT_HadLine Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 30;   //旧默认30,hm默认1调用点再压扁
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //旧剩余比例1→0,得用1-LifetimeCompletion
            //位移全靠框架Position+=Velocity,旧AI里没有那行别加回来
        }

        public override bool PreDraw(SpriteBatch sb) {
            //锚在贴图左中,Rotation绕线条起点转;Glow关采样方块光照
            //NonPremultiplied只乘A,其它桶整色乘Opacity;return false走自定义Draw
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, new Vector2(0, tex.Height / 2f), Scale * new Vector2(1, hm), SpriteEffects.None, 0);
            return false;
        }

        public bool Glow = true;
    }


}
