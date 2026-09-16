using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.Rarities
{
    public class NihilityBlue : ModRarity
    {
        public override Color RarityColor => Color.Blue;

        public override int GetPrefixedRarity(int offset, float valueMult) => Type;
        public static void Draw(Item Item, SpriteBatch spriteBatch, string text, int X, int Y, float rotation,
            Vector2 baseScale, float time, DynamicSpriteFont font) {
            Texture2D glow = CEExtraAssets.Glow;
            Texture2D particle = CEExtraAssets.Ray;
            spriteBatch.UseBlendState_UI(BlendState.Additive);
            Vector2 origin = font.MeasureString(text) * new Vector2(1, 0.6f) * 0.5f;
            float ey = 0;
            spriteBatch.Draw(glow, new Vector2(X, Y + ey) + origin, null, Color.AliceBlue * 0.8f, 0, glow.Size() * 0.5f, origin * 0.02f * new Vector2(1, 0.6f), SpriteEffects.None, 0);
            UnifiedRandom rand = new UnifiedRandom(745367);
            int particleCount = 32;
            for (int i = 0; i < particleCount; i++) {
                Vector2 vec = new Vector2(rand.NextFloat(), 0.5f);
                vec.X = CEUtils.Frac(vec.X - time * 0.18f);
                float alpha = 1;
                if (vec.X < 0.2f)
                    alpha = vec.X / 0.2f;
                if (vec.X > 0.8f)
                    alpha = (1 - vec.X) / 0.2f;
                vec.X = 0.5f + (vec.X - 0.5f) * 1.4f;
                Color clr = Color.Lerp(TextColor1, TextColor2, CEUtils.Parabola(vec.X, 1));
                Vector2 adjPos = new Vector2(X, Y) + vec * new Vector2(font.MeasureString(text).X, 20);
                spriteBatch.Draw(particle, adjPos, null, clr * 0.5f, 0, particle.Size() / 2f, baseScale * new Vector2(0.28f, 0.03f) * rand.NextFloat(0.8f, 1.25f) * alpha, SpriteEffects.None, 0);
            }
            spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);

            for (float r = 0; r < 360; r += 30) {
                Vector2 addVec = MathHelper.ToRadians(r + time).ToRotationVector2() * ((float)(0.5f + Math.Sin(time * 4) * 0.5f) * 1 + 2);
                spriteBatch.DrawString(font, text, new Vector2(X, Y) + addVec, new Color(180, 70, 255) * 0.5f);
            }
            spriteBatch.DrawString(font, text, new Vector2(X, Y), new Color(20, 12, 60));
            spriteBatch.UseBlendState_UI(BlendState.Additive);

            particleCount = 36;
            for (int i = 0; i < particleCount; i++) {
                Vector2 vec = new Vector2(rand.NextFloat(), rand.NextFloat(0f, 0.9f));
                vec.X = CEUtils.Frac(vec.X - time * 0.18f);
                float alpha = 1;
                if (vec.X < 0.2f)
                    alpha = vec.X / 0.2f;
                if (vec.X > 0.8f)
                    alpha = (1 - vec.X) / 0.2f;
                vec.X = 0.5f + (vec.X - 0.5f) * 1.4f;
                Color clr = Color.Lerp(TextColor1, TextColor2, CEUtils.Parabola(vec.X, 1));
                Vector2 adjPos = new Vector2(X, Y) + vec * new Vector2(font.MeasureString(text).X, 20);
                spriteBatch.Draw(particle, adjPos, null, clr * 0.5f, 0, particle.Size() / 2f, baseScale * new Vector2(0.28f, 0.03f) * rand.NextFloat(0.8f, 1.25f) * alpha, SpriteEffects.None, 0);
            }
            spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
        }
        public static float MaxY = 0;
        public static Color TextColor1 = new Color(60, 5, 255);
        public static Color TextColor2 = new Color(245, 200, 255);
        public static void Draw(Item Item, DrawableTooltipLine line) {
            Draw(Item, Main.spriteBatch, line.Text, line.X, line.Y, 0, line.BaseScale, Main.GlobalTimeWrappedHourly, FontAssets.MouseText.Value);
        }
    }
}
