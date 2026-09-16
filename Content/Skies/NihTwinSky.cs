using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>虚寂双子暗幕(基座迁移版):深蓝纯色罩,最远切片一次绘制。</summary>
    public class NihTwinSky : CESkyBase
    {
        protected override bool KeepActive() => Main.LocalPlayer.Entropy().NihSky > 0;

        public override float GetCloudAlpha() => (1f - opacity) * 0.97f + 0.03f;

        protected override void DrawFar(SpriteBatch spriteBatch) {
            //旧实现无门控,0.5 透明度每帧按切片数叠加饱和到 0.75~0.99(随生物群系漂移);
            //单次绘制按其观感取 0.85 校准,留在调用方批次,矩形恰好铺满
            spriteBatch.Draw(CEUtils.pixelTex, CESkyDrawing.CallerFullscreen, new Color(0, 10, 60) * (0.85f * opacity));
        }
    }
}
