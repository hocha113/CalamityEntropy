using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //VoidImpact命中特效,常规PRT PreDraw拉伸贴图,不吃CEVoidScreen虚空metaball也不吃EnablePixelEffect门控
    //跟PRT_Void(PreDraw恒false、CEVoidScreen遍历画)是两条完全独立的管线
    public class PRT_VoidImpactParticle : BasePRT
    {
        public bool Glow = true;   //旧字段,PreDraw没读,Configure仍传保持调用点签名
        public float scaleX = 1f;   //AI里横轴收,PreDraw new Vector2(scaleX*2,1)做冲击条拉伸
        public float scale2 = 0.4f;

        public override string Texture => "CalamityEntropy/Content/Particles/VoidImpactParticle";

        public PRT_VoidImpactParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            Velocity *= 0.92f;
            scaleX *= 0.86f;
            scale2 = float.Lerp(scale2, 1, 0.14f);
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, Color * Opacity, Rotation, tex.Size() / 2f, new Vector2(scaleX * 2, 1) * Scale * scale2, SpriteEffects.None, 0);
            return false;
        }
    }


}
