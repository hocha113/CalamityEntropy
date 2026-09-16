using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //IPixelPass四类spawn少,没开CanPool
    //B/A/vel子步状态+PixelPass开关,池化漏Reset一项斩击就变形,spawn又低不值得赌
    public class PRT_DOracleSlash : BasePRT, IPixelPassPRT
    {
        public bool Glow = true;
        public bool PixelPass { get; set; }
        public float B = -1;
        public float A = -1;
        public float vel = 0.5f;
        public float widthMult = 1;
        public Color centerColor = Color.Black;

        public override string Texture => "CalamityEntropy/Content/Particles/LargeSpark";

        public PRT_DOracleSlash Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 64;
        }

        public override void AI() {
            A += vel;
            vel *= 0.88f;
            B = float.Lerp(B, A, 0.02f);
        }

        void DrawSlash(SpriteBatch sb) {
            Texture2D tex = PRTSharedAssets.LargeSpark.Value;
            float remaining = Lifetime - Time;
            Vector2 size = new Vector2(float.Min(1, remaining / 6f) * Scale / 720f * 0.3f * widthMult, (A - B) * Scale / 720f);
            Vector2 drawPos = Position + Rotation.ToRotationVector2() * Scale * ((A + B) / 2f);
            //第一刀外圈Color,第二刀centerColor*0.6内芯,同DrawSlash里连着画,顺序反了内芯被盖
            sb.Draw(tex, drawPos - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, tex.Size() / 2f, size, SpriteEffects.None, 0);
            //size.y=(A-B)*Scale/720是斩击展开量,B追A的0.02lerp,别当静态宽高去改
            sb.Draw(tex, drawPos - Main.screenPosition, null, centerColor, Rotation + MathHelper.PiOver2, tex.Size() / 2f, size * 0.6f, SpriteEffects.None, 0);
        }

        public override bool PreDraw(SpriteBatch sb) {
            //双通道:PixelPass=true只走EffectLoader像素RT(PreparePixelShader三桶),这层PreDraw直接return
            //普通层和像素层各画一遍DrawSlash,EnablePixelEffect关着时像素那路本来就不显示,别加回退
            if (PixelPass)
                return false;
            DrawSlash(sb);
            return false;
        }

        public void DrawPixelPass(SpriteBatch sb) => DrawSlash(sb);
    }


}
