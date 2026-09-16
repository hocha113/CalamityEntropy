using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class TtizHeadLayer : PlayerDrawLayer
    {
        //犄角贴图在加载期就位,不再每帧走 RequestTex 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Vanity/Ttiz/Horn")]
        internal static Asset<Texture2D> HornTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(Mod, "TerraTiz", EquipType.Head);
        }

        public override bool IsHeadLayer => true;

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Head);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture = HornTex.Value;
            Vector2 headPos = drawInfo.HeadPosition(true) + new Vector2(0, -2);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, new Vector2(-1 * player.direction + texture.Width * 0.5f, texture.Height / 2 + texture.Height / 2 * player.gravDir), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cHead });
        }
    }
    public class TtizBodyLayer : PlayerDrawLayer
    {
        //双形态翅膀贴图在加载期就位,不再每帧走 RequestTex 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Vanity/Ttiz/Wings")]
        internal static Asset<Texture2D> WingsTex;
        [VaultLoaden("CalamityEntropy/Content/Items/Vanity/Ttiz/Wings2")]
        internal static Asset<Texture2D> Wings2Tex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "TerraTiz", EquipType.Body);
        }

        public override bool IsHeadLayer => true;

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Wings);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture = WingsTex.Value;
            if (player.velocity.Y != 0 && (player.mount == null || !player.mount.Active))
                texture = Wings2Tex.Value;
            Vector2 headPos = drawInfo.HeadPosition(true) + new Vector2(player.direction * 2, 12 * player.gravDir);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, drawInfo.colorArmorBody, drawInfo.drawPlayer.fullRotation, new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cBody });
        }
    }
}
