using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_ELineParticle : BasePRT
    {
        public bool Glow = true;
        public Vector2 b;
        public float width = 2;
        public float c;
        public float r;
        public float w = 1;
        public int counter = 0;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            b = default;
            width = 2f;
            c = 0f;
            r = 0f;
            w = 1f;
            counter = 0;
        }

        //PreDraw不走框架Draw,线是drawLine里getExtraTex("white")拉的;Texture借LifeLeaf堵HasAsset,别改成Assets/Extra/white那条像素路
        public override string Texture => "CalamityEntropy/Content/Particles/LifeLeaf";

        public PRT_ELineParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            b = Position;   //尾端锚在spawn点,每帧向当前Position收拢画拖尾
            if (Lifetime <= 0)
                Lifetime = 200;   //旧默认很长,真正死亡看头尾距离+counter
        }

        public override void AI() {
            counter++;
            Velocity *= r;   //r调用点传的速度衰减,默认0就不动
            b = Vector2.Lerp(Position, b, c);   //c是尾端追随系数
            if (CEUtils.getDistance(Position, b) < 2 && counter > 20)
                Kill();
            else
                Lifetime = Time + 2;   //头尾没并拢就续2tick,旧系统靠这个拖着不死
            w *= 0.97f;   //线宽衰减,老值别改
        }

        public override bool PreDraw(SpriteBatch sb) {
            //旧overload硬打Main.spriteBatch,跟传进来的sb是同一个,不用自己End/Begin
            CEUtils.drawLine(Position, b, Color, width * w);
            return false;
        }
    }


}
