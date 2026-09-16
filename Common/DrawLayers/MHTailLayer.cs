using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class MHTailLayer : PlayerDrawLayer
    {
        //尾巴贴图在加载期就位,不再每帧走 getExtraTex 查表
        [VaultLoaden("CalamityEntropy/Assets/Extra/MHTail")]
        internal static Asset<Texture2D> MHTailTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.legs == EquipLoader.GetEquipSlot(Mod, "ScarletKilt", EquipType.Legs) || drawInfo.drawPlayer.legs == EquipLoader.GetEquipSlot(Mod, "KitsunesFan", EquipType.Legs);
        }

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Leggings);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture = MHTailTex.Value;

            Vector2 dpos = drawInfo.HeadPosition();
            dpos += new Vector2(player.direction * -8, 16).RotatedBy(player.fullRotation);
            float rot = player.Entropy().VanityTailRot;

            drawInfo.DrawDataCache.Add(new DrawData(texture, dpos, null, drawInfo.colorArmorLegs, drawInfo.drawPlayer.fullRotation + rot * drawInfo.drawPlayer.direction, new Vector2(drawInfo.playerEffect == SpriteEffects.FlipHorizontally ? 0 : texture.Width, texture.Height / 2f), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cLegs });

        }

    }
}
