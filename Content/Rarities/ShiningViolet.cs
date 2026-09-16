using CalamityEntropy.Common;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using Terraria.Utilities;

namespace CalamityEntropy.Content.Rarities
{
    public class ShiningViolet : ModRarity
    {
        //提示文字发光与闪粒贴图,加载期由 VaultLoaden 赋值,只在物品栏提示绘制里读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/CrystalTextGlow")]
        private static Asset<Texture2D> crystalTextGlowTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/CrystalTextSparkle")]
        private static Asset<Texture2D> crystalTextSparkleTex;
        public override Color RarityColor => Color.Violet;

        public override int GetPrefixedRarity(int offset, float valueMult) => Type;
        public static void Draw(Item Item, SpriteBatch spriteBatch, string text, int X, int Y, Color textColor, Color lightColor, float rotation,
            Vector2 origin, Vector2 baseScale, float time, bool renderTextSparkles, DynamicSpriteFont font) {
            Draw(Item, spriteBatch, text, X, Y, textColor, lightColor, rotation, origin, baseScale, time, renderTextSparkles, font, textColor, lightColor);
        }
        public static void Draw(Item Item, SpriteBatch spriteBatch, string text, int X, int Y, Color textColor, Color lightColor, float rotation,
            Vector2 origin, Vector2 baseScale, float time, bool renderTextSparkles, DynamicSpriteFont font, Color cLinarR, Color cParticle) {
            var crystalTextGlow = crystalTextGlowTex.Value;
            var sparkle = crystalTextSparkleTex.Value;
            var fontSize = font.MeasureString(text);
            var center = fontSize / 2f;

            // this was intended behavior. i'm commenting it out for now because it seems like people do not like it.
            /*
            if (Item.expert)
                textColor = Main.DiscoColor;
            */

            var glowPosition = new Vector2(X + center.X, Y + center.Y / 1.5f);
            float pulsing = 2.5f + (float)Math.Sin(time * 5f);
            for (float f = 0f; f < MathHelper.TwoPi; f += 0.79f) {
                ChatManager.DrawColorCodedString(spriteBatch, font, text, new Vector2(X, Y) + new Vector2(pulsing, 0f).RotatedBy(f + time * 2f % MathHelper.TwoPi), textColor * 0.5f, rotation, origin, baseScale);
            }

            spriteBatch.Draw(crystalTextGlow, glowPosition, null, lightColor, rotation + MathHelper.PiOver2, new Vector2(6f, 33f),
               new Vector2(1.6f, fontSize.X / crystalTextGlow.Height * 1.2f), SpriteEffects.None, 0f);

            float adX = 0;
            for (int i = 0; i < text.Length; i++) {
                Color clr = Color.Lerp(textColor, cLinarR, i / (text.Length - 1f));
                string chr = text[i].ToString();
                ChatManager.DrawColorCodedStringShadow(spriteBatch, font, chr, new Vector2(X + adX, Y), clr * 2f, rotation, origin, baseScale);
                adX += font.MeasureString(chr).X;
            }
            adX = 0;
            for (int i = 0; i < text.Length; i++) {
                string chr = text[i].ToString();
                ChatManager.DrawColorCodedString(spriteBatch, font, chr, new Vector2(X + adX, Y), Color.Black, rotation, origin, baseScale);
                adX += font.MeasureString(chr).X;
            }

            if (!renderTextSparkles)
                return;

            static int Hash(int x) {
                x ^= x >> 16;
                x *= unchecked((int)0x7feb352d);
                x ^= x >> 15;
                x *= unchecked((int)0x846ca68b);
                x ^= x >> 16;
                return x;
            }

            var rand = new UnifiedRandom(Hash((int)(center.X + center.Y)));

            int sparkleCount = rand.Next((int)fontSize.X / 7, (int)fontSize.X / 5) + 1;
            var color2 = lightColor;
            color2.A = 0;
            var sparkleOrigin = new Vector2(15f, 15f);
            for (int i = 0; i < sparkleCount; i++) {
                var v = new Vector2(rand.NextFloat(fontSize.X), rand.NextFloat(fontSize.Y * 0.6f) + 1f);
                float lifeTime = Main.GlobalTimeWrappedHourly * 4f + rand.NextFloat(MathHelper.TwoPi);
                lifeTime %= MathHelper.TwoPi;

                if (lifeTime > MathHelper.TwoPi)
                    continue;

                float sinValue = (float)Math.Sin(lifeTime);
                var white = new Color(200 + lightColor.R / 20, 200 + lightColor.G / 20, 200 + lightColor.B / 20, 255) * sinValue;

                float sparkleRotationSpeed = Main.rand.NextFloat(0.8f, 1.5f); // Unique rotation rate per sparkle
                float sparkleRotation = time * sparkleRotationSpeed;

                spriteBatch.Draw(sparkle, new Vector2(X, Y - lifeTime * MaxY + 3f) + v, null, white, sparkleRotation, sparkleOrigin,
                    lifeTime / MathHelper.TwoPi * 0.3f, SpriteEffects.None, 0f);
                spriteBatch.Draw(sparkle, new Vector2(X, Y - lifeTime * MaxY + 2f) + v, null, white * 0.5f, sparkleRotation, sparkleOrigin,
                    lifeTime / MathHelper.TwoPi, SpriteEffects.None, 0f);

                var scale2 = (float)Math.Sin(lifeTime / MathHelper.PiOver2) + 1f;
                var scale3 = lifeTime / MathHelper.TwoPi;

                scale2 *= 0.2f;
                scale3 *= 0.15f;

                spriteBatch.Draw(sparkle, new Vector2(X, Y - lifeTime * MaxY + 2f) + v, null, color2 * sinValue, sparkleRotation, sparkleOrigin,
                    new Vector2(scale3, scale3) * 1.5f, SpriteEffects.None, 0f);
                spriteBatch.Draw(sparkle, new Vector2(X, Y - lifeTime * MaxY + 2f) + v, null, color2 * sinValue, sparkleRotation, sparkleOrigin,
                    new Vector2(scale2, scale3), SpriteEffects.None, 0f);
                spriteBatch.Draw(sparkle, new Vector2(X, Y - lifeTime * MaxY + 2f) + v, null, color2 * sinValue, sparkleRotation, sparkleOrigin,
                    new Vector2(scale3, scale2), SpriteEffects.None, 0f);
            }
        }
        public static float MaxY = 4.5f;

        public static Color TextClr = Color.Violet * 0.6f;
        public static void Draw(Item Item, string text, int X, int Y, float rotation, Vector2 origin, Vector2 baseScale, Color? textColor = null, Color? lightColor = null, bool? renderTextSparkles = null) {
            Draw(Item, Main.spriteBatch, text, X, Y, Colors.AlphaDarken(textColor ?? TextClr), lightColor ?? Color.Purple, rotation, origin, baseScale, Main.GlobalTimeWrappedHourly,
                renderTextSparkles ?? Config.Instance.TextEffects, FontAssets.MouseText.Value);
        }

        public static void Draw(Item Item, DrawableTooltipLine line) {
            Draw(Item, line.Text, line.X, line.Y, line.Rotation, line.Origin, line.BaseScale);
        }
        public static void Draw(Item Item, DrawableTooltipLine line, Color cText, Color cLinarR, Color cParticle, bool particle = true) {
            Draw(Item, Main.spriteBatch, line.Text, line.X, line.Y,
            cText, cText * 0.6f, line.Rotation,
            line.Origin, line.BaseScale, Main.GlobalTimeWrappedHourly,
             particle, FontAssets.MouseText.Value, cLinarR, cParticle);
        }
    }
}
