using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class SnowgraveScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player) => Main.LocalPlayer.Entropy().snowgrave > 0;

        public override void SpecialVisuals(Player player, bool isActive) {
            player.ManageSpecialBiomeVisuals("CalamityEntropy:Snowgrave", isActive);
        }
    }

    /// <summary>雪葬白幕(基座迁移版):双层渐变罩,最远切片一次绘制。</summary>
    public class SnowgraveSky : CESkyBase
    {
        //整屏渐变贴图,加载期由 VaultLoaden 赋值,绘制里不再走 getExtraTex
        [VaultLoaden("CalamityEntropy/Assets/Extra/WhiteFade")]
        private static Asset<Texture2D> whiteFadeTex;

        protected override float FadeInStep => 0.025f;

        protected override bool KeepActive() => Main.LocalPlayer.Entropy().snowgrave > 0;

        public override Color OnTileColor(Color inColor)
            => Color.Lerp(inColor, new Color(230, 230, 255, inColor.A), opacity);

        public override float GetCloudAlpha() => (1f - opacity) * 0.5f + 0.5f;

        protected override void DrawFar(SpriteBatch spriteBatch) {
            Texture2D tex = whiteFadeTex.Value;
            //旧实现无门控,每帧按切片数(约 4~13)叠加到近饱和;单次绘制按其观感上调透明度
            Color c1 = new Color(180, 200, 255, (int)(255 * opacity));
            Color c2 = new Color(50, 50, 255, (int)(255 * opacity * 0.8f));
            Rectangle full = CESkyDrawing.CallerFullscreen;

            CESkyDrawing.BeginCallerSpace(spriteBatch, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp);
            spriteBatch.Draw(tex, full, null, c2, 0, Vector2.Zero, SpriteEffects.FlipVertically, 0);
            spriteBatch.Draw(tex, full, c1);
            CESkyDrawing.RestoreCallerBatch(spriteBatch);
        }
    }
}
