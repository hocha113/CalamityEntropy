using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_HadCircle : BasePRT
    {
        public float CScale = 1;
        public bool Glow = true;
        private int remaining = 16;   //自己倒计时到0 Kill,不靠Lifetime到点

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            CScale = 1f;
            Glow = true;
            remaining = 16;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/HadCircle";

        public PRT_HadCircle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            remaining = 16;   //自己数16tick到0 Kill,跟Lifetime到点是两套钟
            if (Lifetime <= 0)
                Lifetime = 20;   //Lifetime只是兜底,真死亡看remaining
        }

        public override void AI() {
            if (remaining > 8)
                remaining -= 2;
            else if (remaining > 0)
                remaining -= 1;
            else
                Kill();

            //sqrt曲线鼓起来再收,不是线性淡出
            float t = (float)Math.Sqrt(1 - Math.Abs(8f - remaining) / 8f);
            Opacity = t;
            Scale = t * 0.94f * CScale;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //Glow关走方块光照,NonPremultiplied只乘A
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            Color clr = Color;
            if (!Glow)
                clr = Terraria.Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Vector2 origin = tex.Size() / 2f;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, origin, Scale, SpriteEffects.None, 0);
            return false;
        }
    }

    public class PRT_HadCircle2 : BasePRT
    {
        public float CScale = 1;
        public float scale2 = 1;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            CScale = 1f;
            scale2 = 1f;
            Glow = true;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/BloomRing";

        public PRT_HadCircle2 Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 16;   //旧默认16,纯LifetimeCompletion驱动
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //旧剩余比例1→0
            Scale = LifetimeCompletion * 2.4f * CScale;   //越大环扩得越快
        }

        public override bool PreDraw(SpriteBatch sb) {
            //BloomRing叠两层是Calamity原值,删一层变薄一圈
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            float drawScale = Scale * scale2;
            Color clr = Color;
            if (!Glow)
                clr = Terraria.Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Position - Main.screenPosition;
            sb.Draw(tex, drawPos, null, clr, Rotation, origin, drawScale, SpriteEffects.None, 0);
            sb.Draw(tex, drawPos, null, clr, Rotation, origin, drawScale, SpriteEffects.None, 0);   //BloomRing原版叠两层
            return false;
        }

        public bool Glow = true;
    }



}
