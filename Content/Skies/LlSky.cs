using CalamityEntropy.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>次元透镜彩蛋天空(基座迁移版):把桌面壁纸等比铺满天幕,最远切片一次绘制。</summary>
    public class LlSky : CESkyBase
    {
        protected override float FadeInStep => 0.025f;

        protected override bool KeepActive() => Main.LocalPlayer.Entropy().llSky > 0;

        public override Color OnTileColor(Color inColor) => Color.Lerp(inColor, Color.White, opacity);

        public override float GetCloudAlpha() => 1f - opacity;

        protected override void DrawFar(SpriteBatch spriteBatch) {
            Texture2D txd = WallpaperHelper.getWallpaper();
            //cover 等比:先满宽,高度不够再放大到满高(调用方空间,任意分辨率恰好铺满)
            float scale = Main.screenWidth / (float)txd.Width;
            if (txd.Height * scale < Main.screenHeight)
                scale = Main.screenHeight / (float)txd.Height;
            spriteBatch.Draw(txd, Main.ScreenSize.ToVector2() / 2f, null, Color.White * opacity, 0, txd.Size() / 2f, scale, SpriteEffects.None, 0);
        }
    }
}
