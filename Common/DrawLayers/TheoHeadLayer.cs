using CalamityEntropy.Content.Items.Vanity;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class TheoHeadLayer : PlayerDrawLayer
    {
        //头饰贴图在加载期就位,不再每帧走 getExtraTex 查表
        [VaultLoaden("CalamityEntropy/Assets/Extra/TheoHead")]
        internal static Asset<Texture2D> TheoHeadTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.GetModPlayer<VanityModPlayer>().TheocracyMark;
        }

        public override bool IsHeadLayer => true;

        public override Position GetDefaultPosition() {
            return new AfterParent(PlayerDrawLayers.Head);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture = TheoHeadTex.Value;

            Vector2 headPos = drawInfo.HeadPosition(true);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, new Vector2(drawInfo.playerEffect == SpriteEffects.FlipHorizontally ? texture.Width - 38 : 38, texture.Height / 2f - 1), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.GetModPlayer<VanityModPlayer>().TheocrazyDye });

        }

    }
}
