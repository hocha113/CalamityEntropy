using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //EParticle迁来的Smoke,Solar Storm蒸汽环一圈圈批量spawn这粒,textureType=1时切Circle贴图
    public class PRT_Smoke : BasePRT
    {
        public bool Glow = true;
        public int timeleftmax = 200;
        public float scaleEnd = -1;
        public float scaleStart = -1;
        public float vc = 1;
        public bool colorTrans = false;
        private bool setColor = true;
        public Color endColor = Color.White;
        private Color startColor = Color.White;
        public bool NoAlphaFade = false;
        public int textureType = 0;

        public override bool CanPool => true;

        public override void Reset() {
            //CanPool,setColor/scaleStart得清,不然复用粒子颜色插值从脏startColor起跑
            base.Reset();
            Glow = true;
            timeleftmax = 200;
            scaleEnd = -1;
            scaleStart = -1;
            vc = 1;
            colorTrans = false;
            setColor = true;
            endColor = Color.White;
            startColor = Color.White;
            NoAlphaFade = false;
            textureType = 0;
        }

        public override string Texture => textureType == 0
            ? "CalamityEntropy/Content/Particles/Smoke"
            : "CalamityEntropy/Assets/Extra/Circle";

        public PRT_Smoke Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0) {
                Lifetime = lifetime;
                timeleftmax = lifetime;
            }
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = timeleftmax;
            setColor = true;
            scaleStart = -1;
        }

        public override void AI() {
            float remaining = (Lifetime - Time) / (float)timeleftmax;   //旧进度1→0剩余比例,没用LifetimeCompletion

            if (setColor) {
                setColor = false;
                startColor = Color;
            }
            if (colorTrans)
                Color = Color.Lerp(startColor, endColor, 1f - remaining);
            if (scaleStart < 0)
                scaleStart = Scale;
            if (scaleEnd >= 0)
                Scale = float.Lerp(scaleStart, scaleEnd, 1f - remaining);

            Velocity *= vc;   //vc=速度衰减,老字段名,调用点太多懒得改
            if (!NoAlphaFade)
                Opacity = remaining;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            //NonPremultiplied桶只乘A通道,跟旧EParticle三分支语义一致,别改成clr*=Opacity
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            //textureType运行时可切Smoke/Circle,PreDraw自己拿图,别改成PRT_IDToTexture
            Texture2D tex = textureType == 0
                ? PRTSharedAssets.Smoke.Value
                : PRTExtraTextures.Circle.Value;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation,
                tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
