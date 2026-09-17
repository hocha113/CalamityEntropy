using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //旧PixelParticle,2×2白块贝塞尔轨迹,走常规PRT AlphaBlend桶
    //类名容易和IPixelPassPRT/PreparePixelShader那条像素RT通道搞混,这类完全不接屏幕特效管线,EnablePixelEffect关着也照常画
    public class PRT_Pixel : BasePRT
    {
        public float lifePercent;
        public float j;
        public Vector2 startPos;
        public Vector2 midPos;
        public Vector2 endPos;
        public Color _startColor;
        public Color _endColor;

        public override bool CanPool => true;

        //池化复用,端点/颜色忘Reset=下一条轨迹从脏坐标出发
        public override void Reset() {
            base.Reset();
            lifePercent = 0f;
            j = 0f;
            startPos = default;
            midPos = default;
            endPos = default;
            _startColor = default;
            _endColor = default;
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/white";

        //位置由贝塞尔插值算,框架自动Position+=Velocity必须关
        public override bool ShouldUpdatePosition() => false;

        public PRT_Pixel Configure(Vector2 start, Vector2 mid, Vector2 end, float lifeTime, Color startColor, Color endColor) {
            startPos = start;
            midPos = mid;
            endPos = end;
            j = 1f / lifeTime;   //lifePercent自推,没用LifetimeCompletion
            _startColor = startColor;
            _endColor = endColor;
            return this;
        }

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AlphaBlend;
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 200;
        }

        public override void AI() {
            lifePercent += j;
            if (lifePercent > 1)
                lifePercent = 1;
            if (lifePercent >= 1f)
                Kill();
        }

        public override bool PreDraw(SpriteBatch sb) {
            Vector2 a = Vector2.Lerp(startPos, midPos, lifePercent);
            Vector2 b = Vector2.Lerp(midPos, endPos, lifePercent);
            Vector2 drawPos = Vector2.Lerp(a, b, lifePercent);
            Texture2D pixel = PRTExtraTextures.White.Value;
            sb.Draw(pixel, drawPos - Main.screenPosition, null, Color.Lerp(_startColor, _endColor, lifePercent), 0, pixel.Size() / 2, 2, SpriteEffects.None, 0);
            return false;
        }
    }
}
