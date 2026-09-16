using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Terraria;

namespace CalamityEntropy.Utilities
{
    class WallpaperHelper
    {
        //获取壁纸，为了给维度透镜的场景使用
        private const int SPI_GETDESKWALLPAPER = 0x0073;
        private const int MAX_PATH = 260;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);


        public static string GetDesktopWallpaper() {
            if (!OperatingSystem.IsWindows()) {
                return null;
            }
            return CopyWallpaperToTemp();
        }

        public static string CopyWallpaperToTemp() {
            StringBuilder wallpaperPath = new StringBuilder(MAX_PATH);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaperPath, 0);
            string originalPath = wallpaperPath.ToString();
            string tempPath = Path.Combine(Main.SavePath, "CalamityEntropy/Wallpaper.jpg");

            try {
                File.Copy(originalPath, tempPath, true);
                return tempPath;
            } catch (UnauthorizedAccessException) {
                return null;
            }

        }

        public static Texture2D wallpaper = null;
        public static Texture2D getWallpaper() {
            if (wallpaper != null) {
                return wallpaper;
            }
            try {
                string wallpaperPath = GetDesktopWallpaper();
                if (wallpaperPath == null) {
                    wallpaper = CEExtraAssets.white;
                    return wallpaper;
                }
                GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
                if (File.Exists(wallpaperPath)) {
                    using (FileStream stream = new FileStream(wallpaperPath, FileMode.Open)) {
                        wallpaper = Texture2D.FromStream(graphicsDevice, stream);
                        return wallpaper;
                    }
                }
                else {
                    wallpaper = CEExtraAssets.white;
                }
            } catch { wallpaper = CEExtraAssets.white; }
            return wallpaper;

        }
    }
}