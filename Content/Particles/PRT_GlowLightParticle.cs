using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_GlowLightParticle : BasePRT
    {
        public bool Glow = true;
        public int HideTime = 20;
        public bool AlphaShrink = true;
        public float scale2 = 1;
        public Color lightColor = Color.White * 0.2f;

        public override bool CanPool => true;

        //CanPool复用,HideTime/AlphaShrink/scale2/lightColor都得回默认
        public override void Reset() {
            base.Reset();
            Glow = true;
            HideTime = 20;
            AlphaShrink = true;
            scale2 = 1f;
            lightColor = Color.White * 0.2f;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/GlowLight";

        public PRT_GlowLightParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            //AlphaShrink走remaining/HideTime,否则scale2吃LifetimeCompletion,两路进度别混
            if (AlphaShrink)
                Opacity = (Lifetime - Time) / (float)HideTime;
            else
                scale2 = 1f - LifetimeCompletion;
            if (Opacity > 1)
                Opacity = 1;
            Velocity *= 0.96f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, lightColor * Opacity, 0, tex.Size() / 2f, Scale * 0.65f * scale2, SpriteEffects.None, 0);
            sb.Draw(tex, Position - Main.screenPosition, null, Color * Opacity, 0, tex.Size() / 2f, Scale * 0.08f * scale2, SpriteEffects.None, 0);
            return false;
        }
    }


}
