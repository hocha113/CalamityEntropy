using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class OathBannerDrawLayer : PlayerDrawLayer
    {
        //旗帜贴图在加载期就位,不再每帧走 RequestTex 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Accessories/Oath/OathBannerHoldout")]
        internal static Asset<Texture2D> BannerTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.Entropy().oathBannerVisual;
        }

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Wings);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D tex = BannerTex.Value;
            int MaxFrame = 8;
            Vector2 offset = drawInfo.GetFrameOrigin() + new Vector2(player.width, player.height);
            int th = tex.Height / MaxFrame;
            int frame = player.Entropy().OathBannerFrameCount;
            Vector2 origin = new Vector2(tex.Width / 2 - 44, th / 2 + (player.gravDir < 0 ? 16 : 38));
            if (player.direction * player.gravDir > 0)
                origin.X = tex.Width - origin.X;
            Rectangle rect = new Rectangle(0, th * frame, tex.Width, th - 2);
            float rot = player.Entropy().FlagRot + (player.gravDir > 0 ? 0 : MathHelper.Pi);
            drawInfo.DrawDataCache.Add(new DrawData(tex, offset, rect, drawInfo.colorArmorBody, rot, origin, 1, player.direction * player.gravDir < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally) { shader = player.Entropy().oathBannerDye });
        }
    }
}
