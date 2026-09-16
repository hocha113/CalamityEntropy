using CalamityEntropy;
using CalamityEntropy.Content.Items.Vanity;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CoHHeadDrawLayer.Common.DrawLayers
{
    public class CoHHeadDrawLayer : PlayerDrawLayer
    {
        //发饰贴图在加载期就位,不再每帧走 RequestTex 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Vanity/CrystalofHeart_Hair")]
        internal static Asset<Texture2D> HairTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(Mod, "CrystalofHeart", EquipType.Head);
        }

        public override bool IsHeadLayer => true;

        public override Position GetDefaultPosition() {
            return new AfterParent(PlayerDrawLayers.Head);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            var mp = player.GetModPlayer<VanityModPlayer>();
            Texture2D texture = HairTex.Value;
            Vector2 headPos = drawInfo.HeadPosition(true);

            Vector2 offset = new Vector2(-2, 0);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, new Vector2(texture.Width * 0.5f - player.direction * offset.X, texture.Height * 0.5f - offset.Y), 1, drawInfo.playerEffect));
        }

    }
}
