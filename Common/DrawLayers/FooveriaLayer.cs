using CalamityEntropy.Content.Items.Donator;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class FooveriaLayer : PlayerDrawLayer
    {
        //武器与辉光贴图在加载期就位,不再每帧走 RequestTex 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Donator/Fooveria")]
        internal static Asset<Texture2D> FooveriaTex;
        [VaultLoaden("CalamityEntropy/Content/Items/Donator/FooveriaGlow")]
        internal static Asset<Texture2D> FooveriaGlowTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.HeldItem.ModItem != null && drawInfo.drawPlayer.HeldItem.ModItem is Fooveria && drawInfo.drawPlayer.itemAnimation == 0;
        }

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Wings);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            Player player = drawInfo.drawPlayer;
            Texture2D tex = FooveriaTex.Value;
            Texture2D tex2 = FooveriaGlowTex.Value;
            float GlowAlpha = 0;
            if (player.Entropy().noItemTime >= 15 && player.Entropy().noItemTime <= 26) {
                GlowAlpha = Utils.Remap(player.Entropy().noItemTime, 15, 26, 0, 1);
            }
            if (player.Entropy().noItemTime > 26)
                return;
            Vector2 offset = drawInfo.GetFrameOrigin() + new Vector2(drawInfo.drawPlayer.width - drawInfo.drawPlayer.direction * 6, drawInfo.drawPlayer.height * 0.5f);
            float rot = 2.12f;
            if (player.direction < 0)
                rot = (rot.ToRotationVector2() * new Vector2(-1, 1)).ToRotation();
            rot += MathHelper.PiOver4 * player.direction;
            drawInfo.DrawDataCache.Add(new DrawData(tex, offset, null, Color.White, rot, tex.Size() * 0.5f, 1, player.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically));
            drawInfo.DrawDataCache.Add(new DrawData(tex2, offset, null, Color.White * GlowAlpha, rot, tex.Size() * 0.5f, 1, player.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically));
        }

    }
}
