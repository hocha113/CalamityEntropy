using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{


    public class PRT_ImpactParticle : BasePRT
    {
        public bool Glow = true;
        public float sadd = 0.1f;

        public override bool CanPool => true;

        //CanPool复用,Glow/sadd回默认,sadd在AI里还会*=0.9
        public override void Reset() {
            base.Reset();
            Glow = true;
            sadd = 0.1f;
        }

        //Texture占位,PreDraw走PRTExtraTextures.Impact2
        public override string Texture => "CalamityEntropy/Assets/Extra/Impact2";

        public PRT_ImpactParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 120;   //旧Impact默认120,到点Kill是框架的事
        }

        public override void AI() {
            Scale += sadd;
            sadd *= 0.9f;   //Scale增速衰减,旧字段名sadd没改
            //没显式Kill,Color*=0.96每帧淡出,alpha见底视觉上没了但Lifetime还在跑
            Color *= 0.96f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //不走自定义批次,旧drawAll也是普通sb.Draw;Glow=false才吃地块光照
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            //NonPremultiplied只乘A,blend走Configure尾参mode
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Texture2D tex = PRTExtraTextures.Impact2.Value;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation,
                tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
