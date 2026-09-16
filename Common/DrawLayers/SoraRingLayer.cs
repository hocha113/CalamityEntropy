using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class SoraRingLayer : PlayerDrawLayer
    {
        //光环贴图在加载期就位,不再每帧走 getExtraTex 查表
        [VaultLoaden("CalamityEntropy/Assets/Extra/sRing")]
        internal static Asset<Texture2D> RingTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            var drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(Mod, "MysteriousBook", EquipType.Head);
        }

        public override bool IsHeadLayer => true;

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Head);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture;
            Vector2 headPos;

            texture = RingTex.Value;
            headPos = drawInfo.HeadPosition(true);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, Color.White, drawInfo.drawPlayer.headRotation, new Vector2(texture.Width / 2 - 3 * player.direction, texture.Height / 2 + 25 * player.gravDir), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cHead });

        }

    }
}
