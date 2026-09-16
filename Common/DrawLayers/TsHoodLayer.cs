using CalamityEntropy.Content.Items.Vanity;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class TsHoodLayer : PlayerDrawLayer
    {
        //兜帽贴图在加载期就位,不再每帧走 RequestTex 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Vanity/TsumugisHood_Hood")]
        internal static Asset<Texture2D> HoodTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            var drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(Mod, "TsumugisHood", EquipType.Head) && drawPlayer.GetModPlayer<VanityModPlayer>().SpecialFlag == 0;
        }

        public override bool IsHeadLayer => true;

        public override Position GetDefaultPosition() {
            return new AfterParent(PlayerDrawLayers.Head);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture;
            Vector2 headPos;

            texture = HoodTex.Value;
            headPos = drawInfo.HeadPosition(true);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, new Vector2(texture.Width / 2 - 1 * player.direction, texture.Height / 2 + 9 * player.gravDir), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cHead });
        }
    }
}
