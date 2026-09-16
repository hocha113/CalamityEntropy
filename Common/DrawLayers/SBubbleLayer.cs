using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class SBubbleLayer : PlayerDrawLayer
    {
        //气泡贴图在加载期就位,不再每帧走 RequestTex 查表
        [VaultLoaden("CalamityEntropy/Content/Items/Books/BookMarks/SBMBubble")]
        internal static Asset<Texture2D> BubbleTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.Entropy().SulphurousBubble;
        }

        public override Position GetDefaultPosition() {
            return PlayerDrawLayers.AfterLastVanillaLayer;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            Texture2D tex = BubbleTex.Value;
            Vector2 scale = new Vector2(0.8f + 0.2f * ((float)((Math.Sin(Main.GlobalTimeWrappedHourly * 2) * 0.5f) + 0.5f)), 0.8f + 0.2f * ((float)((Math.Sin(Main.GlobalTimeWrappedHourly * 2 + MathHelper.PiOver2) * 0.5f) + 0.5f)));
            drawInfo.DrawDataCache.Add(new DrawData(tex, drawInfo.GetFrameOrigin() + new Vector2(drawInfo.drawPlayer.width, drawInfo.drawPlayer.height) + new Vector2(0, -10), null, Color.White, 0, tex.Size() / 2f, scale, SpriteEffects.None));
        }
    }
}
