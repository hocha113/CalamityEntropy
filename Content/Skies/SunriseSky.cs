using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class SunriseSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override bool IsSceneEffectActive(Player player) => Main.LocalPlayer.Entropy().SunriseScene > 0;
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Cliff");

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals("CalamityEntropy:SunriseSky", isActive);
        }
    }

    /// <summary>
    /// 悬崖日出全景天空(基座迁移版):渐变天光 + 三层云 + 悬崖 + 太阳 + 八层远景。
    /// 全程画在调用方(背景矩阵)空间,缩放系数用浮点除法,任意分辨率等比铺满;
    /// 旧实现的 UseSampleState/ExitShaderRegion 会切进 GameViewMatrix 错误空间并泄漏,已废弃。
    /// </summary>
    public class SunriseSky : CESkyBase
    {
        public static Color fColor = new Color(255, 240, 80);
        //专用服务器上这些字段保持 null,与原先 !Main.dedServ 守卫等价
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/Background")]
        public static Texture2D Background;
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/Cliffs")]
        public static Texture2D Cliffs;
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/CloudsBack1")]
        public static Texture2D CloudsBack1;
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/CloudsBack2")]
        public static Texture2D CloudsBack2;
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/CloudsFore")]
        public static Texture2D CloudsFore;
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/CloudsMid")]
        public static Texture2D CloudsMid;
        //Field0~Field7 八张远景层,按数字后缀批量加载
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/Field", 0, 8, AssetMode = AssetMode.TextureValueArray)]
        public static Texture2D[] Fields;
        [VaultLoaden("CalamityEntropy/Assets/Sunrise/Sun")]
        public static Texture2D Sun;

        public float sunPos = 0;

        protected override bool KeepActive() => Main.LocalPlayer.Entropy().SunriseScene > 0;

        public override Color OnTileColor(Color inColor) {
            return Color.Lerp(inColor, Color.Lerp(new Color(25, 50, 50, 255), fColor, sunPos * 0.5f + 0.5f), opacity);
        }

        public override float GetCloudAlpha() => 1f - opacity;

        protected override void UpdatePayload(GameTime gameTime) {
            //太阳高度由时刻推出;OnTileColor 在更新阶段读它,放这里而不是 Draw
            if (Main.dayTime)
                sunPos = (float)(Math.Cos(Main.time / 54000.0 * MathHelper.TwoPi - MathHelper.Pi) * 0.5 + 0.5);
            else
                sunPos = -(float)(Math.Cos(Main.time / 32400.0 * MathHelper.TwoPi - MathHelper.Pi) * 0.5 + 0.5);
        }

        protected override void DrawFar(SpriteBatch spriteBatch) {
            float time = Main.GameUpdateCount;
            Color lColor = Color.Lerp(new Color(54, 50, 50, 255), Color.White, sunPos * 0.5f + 0.5f);
            float worldHeight = Main.maxTilesY * 16;
            float xOffset = CESkyDrawing.RealScreenPosition.X;
            //等比缩放:旧实现是 int/int 整除,缩放随分辨率阶梯跳变,某些分辨率露边
            float scale = Main.screenWidth / (float)Cliffs.Width;
            Rectangle fullscreen = CESkyDrawing.CallerFullscreen;

            CESkyDrawing.BeginCallerSpace(spriteBatch, BlendState.AlphaBlend, SamplerState.PointWrap);
            float offsetn = time * 0.1f;
            spriteBatch.Draw(CEUtils.pixelTex, fullscreen, Color.Lerp(new Color(52, 20, 12, 255), new Color(254, 224, 79), sunPos * 0.5f + 0.5f) * opacity);

            spriteBatch.Draw(CloudsBack2, Vector2.UnitY * -60 * scale, new Rectangle((int)offsetn, 0, CloudsBack2.Width, CloudsBack2.Height), lColor * opacity, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            offsetn = time * 0.25f;
            spriteBatch.Draw(CloudsBack1, Vector2.Zero, new Rectangle((int)offsetn, 0, CloudsBack1.Width, CloudsBack1.Height), lColor * opacity, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            offsetn = time * 0.5f;
            spriteBatch.Draw(CloudsMid, new Vector2(0, 15 * scale), new Rectangle((int)offsetn, 0, CloudsMid.Width, CloudsMid.Height), lColor * opacity, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            offsetn = time * 0.8f;
            spriteBatch.Draw(CloudsFore, Vector2.Zero, new Rectangle((int)offsetn, 0, CloudsFore.Width, CloudsFore.Height), lColor * opacity, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

            CESkyDrawing.BeginCallerSpace(spriteBatch, BlendState.AlphaBlend, SamplerState.PointClamp);
            offsetn = 6;
            spriteBatch.Draw(Cliffs, new Vector2(0, 100 * scale), new Rectangle((int)offsetn, 0, (int)(Main.screenWidth / scale) + 1, Cliffs.Height), lColor * opacity, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            spriteBatch.Draw(Sun, new Vector2(236 * scale, (138 - sunPos * 20) * scale), null, Color.White * opacity, 0, Sun.Size().Half(), scale, SpriteEffects.None, 0);
            spriteBatch.Draw(CEUtils.pixelTex, new Vector2(0, 137 * scale), fullscreen, Color.Lerp(new Color(40, 32, 16, 255), new Color(247, 210, 43), sunPos * 0.5f + 0.5f) * opacity);

            CESkyDrawing.BeginCallerSpace(spriteBatch, BlendState.AlphaBlend, SamplerState.PointWrap);
            for (int i = 0; i < 8; i++) {
                offsetn = xOffset * ((i / 7f) * (i / 7f) * (i / 7f) * (i / 7f) * (i / 7f) * (i / 7f) * 0.04f);
                float yset = (float)Math.Pow(float.Max(0, worldHeight * 0.22f - CESkyDrawing.RealScreenPosition.Y) * ((i / 7f) * (i / 7f) * (i / 7f) * (i / 7f) * (i / 7f) * (i / 7f) * 16f), 0.52f) * 0.3f;
                spriteBatch.Draw(Fields[i], Vector2.UnitY * (138 + yset) * scale, new Rectangle((int)offsetn, 0, fullscreen.Width, Fields[i].Height), lColor * opacity * (0.4f + 0.6f * (i / 7f)), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
            CESkyDrawing.RestoreCallerBatch(spriteBatch);
        }
    }
}
