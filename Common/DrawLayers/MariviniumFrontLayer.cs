using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class MariviniumFrontLayer : PlayerDrawLayer
    {
        //前甲贴图在加载期就位,不再每帧走 Request 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Armor/Marivinium/Front")]
        internal static Asset<Texture2D> FrontTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "MariviniumBodyArmor", EquipType.Body);
        }

        public override Position GetDefaultPosition() {
            return new AfterParent(PlayerDrawLayers.ArmOverItem);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            Texture2D front = FrontTex.Value;
            Player player = drawInfo.drawPlayer;
            Vector2 offset = drawInfo.GetFrameOrigin() + new Vector2(5 * player.direction, 1) + new Vector2(drawInfo.drawPlayer.width, drawInfo.drawPlayer.height - 16) + Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height] * drawInfo.drawPlayer.gravDir;
            drawInfo.DrawDataCache.Add(new DrawData(front, offset, null, drawInfo.colorArmorBody, player.fullRotation, (front.Size() / 2f), 1, drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally) { shader = drawInfo.drawPlayer.cBody });

        }

    }
}
