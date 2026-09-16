using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //PRT_Light的轻量bloom替身,NowScale每帧+=ScaleAdd做膨胀,寿命默认20很短
    public class PRT_LightAlt : BasePRT
    {
        public bool Glow = true;
        public Vector2 ScaleAdd = Vector2.Zero;
        public Vector2 NowScale = Vector2.One;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            ScaleAdd = Vector2.Zero;
            NowScale = Vector2.One;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/PRT_Light2";

        public PRT_LightAlt Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 20;
        }

        public override void AI() {
            NowScale += ScaleAdd;   //每tick累加,旧EParticle没框架位移就靠这个鼓起来
        }

        public override bool PreDraw(SpriteBatch sb) {
            float remaining = 1f - LifetimeCompletion;
            Texture2D tex = PRTSharedAssets.PRT_Light2.Value;   //bloom贴图VaultLoaden,跟PRT_Light.BloomTex同一张
            sb.Draw(tex, Position - Main.screenPosition, null, Color * Opacity * remaining, Rotation,
                tex.Size() / 2f, NowScale * Scale, SpriteEffects.None, 0);   //remaining乘Opacity,双通道淡出
            return false;
        }
    }


}
