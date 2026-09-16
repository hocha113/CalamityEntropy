using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //EParticle迁来的CrystalGlow,EmberSpike命中闪晶,双旋转叠两层
    public class PRT_CrystalGlow : BasePRT
    {
        public bool Glow = true;
        public float r1 = 0;
        public float r2 = 0;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            r1 = 0f;
            r2 = 0f;
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/CrystalGlow";

        public PRT_CrystalGlow Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            r1 = CEUtils.randomRot();   //随机角放SetProperty,CanPool复用不会重跑字段初始化器
            r2 = CEUtils.randomRot();
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //只淡出不改Scale,跟GlowSpark同款remaining写法
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTExtraTextures.CrystalGlow.Value;   //Assets/Extra走VaultLoaden,不走Texture属性加载
            Color clr = Color * Opacity;
            sb.Draw(tex, Position - Main.screenPosition, null, clr, r1, tex.Size() * 0.5f, Scale, SpriteEffects.None, 0);
            sb.Draw(tex, Position - Main.screenPosition, null, clr, r2, tex.Size() * 0.5f, Scale, SpriteEffects.None, 0);   //r1/r2独立随机角,视觉上像两片晶体
            return false;
        }
    }


}
