using CalamityEntropy.Content.Items.Accessories;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class PLWingDrawLayer : PlayerDrawLayer
    {
        //翅膀帧动画数组化(f0~f7)+ 收拢单帧 f,绘制时按下标取帧,消灭每帧字符串拼接
        [VaultLoaden("CalamityEntropy/Assets/Extra/PLWing/f", 0, 8, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] WingFrames;
        [VaultLoaden("CalamityEntropy/Assets/Extra/PLWing/f")]
        internal static Asset<Texture2D> WingIdleTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead || (drawInfo.drawPlayer.Entropy().vanityWing != null && !(drawInfo.drawPlayer.Entropy().vanityWing.ModItem is PhantomLightWing)))
                return false;
            return drawInfo.drawPlayer.Entropy().hasAccVisual("PLWing");
        }

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Wings);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            if (player.Entropy().wingData.FrameCount >= player.Entropy().wingData.MaxFrame) {
                player.Entropy().wingData.FrameCount = 0;
            }
            Texture2D tex = player.Entropy().wingData.FrameCount == -1 ? WingIdleTex.Value : WingFrames[player.Entropy().wingData.FrameCount];
            Vector2 offset = drawInfo.GetFrameOrigin() + new Vector2(drawInfo.drawPlayer.width, drawInfo.drawPlayer.height);
            drawInfo.DrawDataCache.Add(new DrawData(tex, offset, null, drawInfo.colorArmorBody, 0, new Vector2(drawInfo.drawPlayer.direction == 1 ? 52 : tex.Width - 52, 54), 1, drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally) { shader = drawInfo.drawPlayer.cWings });
        }

    }
}
