using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //EXPLOSION类名全大写,贴图文件名绑着,迁移纪律别美化
    //常规PRT桶,跟屏幕特效管线/EnablePixelEffect无关; RustyGrenade走NonPremultiplied但PreDraw直接画Color没乘Opacity,老代码就这样的
    public class PRT_EXPLOSION : BasePRT
    {
        public bool Glow = true;
        public int frame = 0;

        public override string Texture => "CalamityEntropy/Content/Particles/EXPLOSION";

        public PRT_EXPLOSION Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 96;
        }

        public override void AI() {
            frame++;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            //spritesheet 11列×多行,每格240×135,frame每tick+1不按LifetimeCompletion切
            Rectangle rect = new Rectangle(frame % 11 * 240, frame / 11 * 135, 240, 135);
            sb.Draw(tex, Position - Main.screenPosition, rect, Color, Rotation, new Vector2(120, 135 / 2f), Scale, SpriteEffects.None, 0);
            return false;
        }
    }

    //同EXPLOSION网格逻辑,贴图换EXPLOSIONCOSMIC,Gungnir/ToiletVoid那批spawn
    public class PRT_EXPLOSIONCOSMIC : BasePRT
    {
        public int frame = 0;

        public override string Texture => "CalamityEntropy/Content/Particles/EXPLOSIONCOSMIC";

        public PRT_EXPLOSIONCOSMIC Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 96;
        }

        public override void AI() {
            frame++;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            Rectangle rect = new Rectangle(frame % 11 * 240, frame / 11 * 135, 240, 135);
            sb.Draw(tex, Position - Main.screenPosition, rect, Color, Rotation, new Vector2(120, 135 / 2f), Scale, SpriteEffects.None, 0);
            return false;
        }

        public bool Glow = true;
    }



}
