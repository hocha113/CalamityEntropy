using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_ULineParticle : BasePRT
    {
        public bool Glow = true;
        public Vector2 b;
        public float len = 2;
        public float w1 = 0.96f;
        public float w2 = 0.96f;
        public float spd = 0.05f;
        public int counter = 0;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            b = default;
            len = 2f;
            w1 = 0.96f;
            w2 = 0.96f;
            spd = 0.05f;
            counter = 0;
        }

        //同ELineParticle:线走drawLine的white,Texture借LifeLeaf,别指白图走像素通道
        public override string Texture => "CalamityEntropy/Content/Particles/LifeLeaf";

        public PRT_ULineParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            b = Position;   //尾端锚点,逻辑同ELineParticle只是短命版
            if (Lifetime <= 0)
                Lifetime = 4;   //旧默认4,到期前靠Time+4续命
        }

        public override void AI() {
            counter++;
            Velocity *= w2;
            b = Vector2.Lerp(Position, b, w1);
            Opacity -= spd;   //逐帧扣透明度,跟ELine那套距离判死并行
            if (CEUtils.getDistance(Position, b) < 4 && counter > 3 || Opacity < 0)
                Kill();
            else
                Lifetime = Time + 4;   //续命窗口比ELine宽(4 tick),短线闪一下就收
        }

        public override bool PreDraw(SpriteBatch sb) {
            //drawLine拉getExtraTex("white"),Opacity同时压色和线宽
            CEUtils.drawLine(Position, b, Color * Opacity, len * Opacity);
            return false;
        }
    }


}
