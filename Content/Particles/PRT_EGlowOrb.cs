using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //EParticle迁来的GlowOrb,5次叠画做柔光核,remaining走1-Completion
    public class PRT_EGlowOrb : BasePRT
    {
        public bool Glow = true;
        public Color CenterColor = Color.White;
        public float CenterScale = 0.36f;
        public float Slowdown = 0.92f;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            CenterColor = Color.White;
            CenterScale = 0.36f;
            Slowdown = 0.92f;
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/Glow2";

        public PRT_EGlowOrb Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 200;
        }

        public override void AI() {
            Velocity *= Slowdown;   //Slowdown默认0.92,老代码原值,迁移纪律不改
        }

        public override bool PreDraw(SpriteBatch sb) {
            float remaining = 1f - LifetimeCompletion;
            Texture2D tex = PRTExtraTextures.Glow2.Value;   //PreDraw走ExtraTextures VaultLoaden,Texture属性只是同名路径
            Vector2 origin = tex.Size() / 2f;
            for (int i = 0; i < 5; i++)   //5次叠画是旧Draw原样,别嫌浪费改成1次
            {
                sb.Draw(tex, Position - Main.screenPosition, null, Color * remaining, Rotation, origin, Scale, SpriteEffects.None, 0);
                sb.Draw(tex, Position - Main.screenPosition, null, CenterColor * remaining, Rotation, origin, Scale * CenterScale, SpriteEffects.None, 0);
            }
            return false;
        }
    }


}
