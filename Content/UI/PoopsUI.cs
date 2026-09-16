using CalamityEntropy.Content.UI.Poops;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.UI
{
    public static class PoopsUI
    {
        //槽位框与持握图标,加载期由 VaultLoaden 赋值,只在 UI 绘制里读取
        [VaultLoaden("CalamityEntropy/Content/UI/frame")]
        private static Asset<Texture2D> frameTex;
        [VaultLoaden("CalamityEntropy/Content/UI/hold")]
        private static Asset<Texture2D> holdTex;
        public static float holdAnmj = 0;
        public static float holdAnm = 0;
        public static void Draw() {
            Vector2 pos = new Vector2(880, 40);
            int maxShow = 9;
            Vector2 drawPos = pos;
            Texture2D frame = frameTex.Value;
            List<Poop> poops = Main.LocalPlayer.Entropy().poops;
            for (int i = 0; i < maxShow; i++) {
                Main.spriteBatch.Draw(frame, drawPos, null, Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
                drawPos.X += 48;
            }

            drawPos = pos;
            for (int i = 0; i < maxShow; i++) {
                if (i < poops.Count) {
                    Texture2D tex = poops[i].getTexture();
                    Main.spriteBatch.Draw(tex, drawPos + new Vector2(24, 24), null, Color.White, 0, tex.Size() / 2, 2, SpriteEffects.None, 0);
                    drawPos.X += 48;
                }
            }
            float hrot = (float)holdAnm * 0.1f;
            float scale = 1 + holdAnm;
            Main.spriteBatch.UseBlendState_UI(BlendState.NonPremultiplied);
            if (Main.LocalPlayer.Entropy().PoopHold is not null) {
                Texture2D texpoop = Main.LocalPlayer.Entropy().PoopHold.getTexture();
                Main.spriteBatch.Draw(texpoop, pos + new Vector2(24 + maxShow * 48, 24) + new Vector2(0, 8).RotatedBy(hrot), null, Color.White, hrot, texpoop.Size() / 2, 1 * scale, SpriteEffects.None, 0);
            }
            Texture2D hold = holdTex.Value;
            Main.spriteBatch.Draw(hold, pos + new Vector2(24 + maxShow * 48, 24), null, Color.White, hrot, hold.Size() / 2, 2 * scale, SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
            holdAnm += holdAnmj;
            holdAnmj -= 0.04f;
            if (holdAnm < 0) {
                holdAnm = 0;
                holdAnmj = 0;
            }
        }
    }
}
