using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //本身不画,PreDraw恒false,每2tick吐PRT_AbyssalLine(常规PRT斩击线,不是PRT_Abyssal metaball)
    //子线mode:旧逻辑Additive vs 其余全NonPremultiplied,跟EffectLoader/EnablePixelEffect无关
    public class PRT_MultiSlash : BasePRT
    {
        public bool Glow = true;
        public bool useAdditive;
        public Color spawnColor = new Color(190, 190, 255);
        public Color endColor = Color.Blue;
        public float xscale = 0;
        public float xdec = 0.87f;
        public float xadd = 3.2f;
        public float lx = 3;

        public override string Texture => "CalamityEntropy/Content/Particles/AbyssalLine";

        public PRT_MultiSlash Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            useAdditive = mode == PRTDrawModeEnum.AdditiveBlend;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 26;
        }

        public override void AI() {
            if (Time % 2 == 0) {
                var p = PRTLoader.NewParticle<PRT_AbyssalLine>(Position, Vector2.Zero, Color, Scale);
                p.lx = lx * Main.rand.NextFloat(0.6f, 1.4f);
                p.xadd = xadd * Main.rand.NextFloat(0.6f, 1.4f);
                p.xdec = xdec;
                p.xscale = xscale;
                p.endColor = endColor;
                //子粒子mode:旧逻辑Additive vs 其余全进NonPremultiplied
                p.Configure(Opacity, Glow, useAdditive ? PRTDrawModeEnum.AdditiveBlend : PRTDrawModeEnum.NonPremultiplied, CEUtils.randomRot(), 8);
            }
        }

        public override bool PreDraw(SpriteBatch sb) => false;
    }


}
