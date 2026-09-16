using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.Rarities
{
    public class Lunarblight : ModRarity
    {
        public override Color RarityColor => Color.Blue;
        public override int GetPrefixedRarity(int offset, float valueMult) => Type;
        public static void Draw(Item Item, SpriteBatch spriteBatch, string text, int X, int Y, float rotation,
            Vector2 baseScale, float time, DynamicSpriteFont font) {
            Texture2D glow = CEExtraAssets.Glow;
            Texture2D particle = CEExtraAssets.Ray;
            spriteBatch.UseBlendState_UI(BlendState.Additive);
            Vector2 origin = font.MeasureString(text) * new Vector2(1, 0.6f) * 0.5f;
            float ey = CELists.tooltipNameUpList.Contains(Language.ActiveCulture.Name) ? 0 : 4;
            spriteBatch.Draw(glow, new Vector2(X, Y + ey) + origin, null, Color.DeepSkyBlue * 0.6f, 0, glow.Size() * 0.5f, origin * 0.017f * new Vector2(1, 1f), SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            float vx = 0;
            for (int i = 0; i < text.Length; i++) {
                string t = text[i].ToString();
                float p = i / (text.Length - 1f);
                Color clr = TextColor;
                clr = Color.Lerp(p < 0.5f ? ParticleClr1 : ParticleClr2, clr, (p < 0.5f) ? (p * 2) : ((1 - p) * 2));
                for (float r = 0; r < 360; r += 30) {
                    Vector2 addVec = MathHelper.ToRadians(r + time).ToRotationVector2() * ((float)(0.5f + Math.Sin(time * 4) * 0.5f) * 1 + 2);
                    spriteBatch.DrawString(font, t, new Vector2(X + vx, Y) + addVec, clr * 0.25f);
                }
                vx += font.MeasureString(t).X;
            }
            vx = 0;
            for (int i = 0; i < text.Length; i++) {
                string t = text[i].ToString();
                float p = i / (text.Length - 1f);
                Color clr = TextColor;
                clr = Color.Lerp(p < 0.5f ? ParticleClr1 : ParticleClr2, clr, (p < 0.5f) ? (p * 2) : ((1 - p) * 2));
                spriteBatch.DrawString(font, t, new Vector2(X + vx, Y), clr * 2f);
                vx += font.MeasureString(t).X;
            }
            spriteBatch.UseBlendState_UI(BlendState.Additive);

            UnifiedRandom rand = new UnifiedRandom(Item.Name.GetHashCode());
            int particleCount = (int)(font.MeasureString(text).X * 0.1);

            for (int i = 0; i < particleCount; i++) {
                Vector2 vec = new Vector2(rand.NextFloat(), rand.NextFloat());
                vec.Y = CEUtils.Frac(vec.Y - time * 1f);
                float alpha = 1;
                if (vec.Y < 0.25f)
                    alpha = vec.Y / 0.25f;
                if (vec.Y > 0.75f)
                    alpha = (1 - vec.Y) / 0.25f;
                vec.Y = 0.5f + (vec.Y - 0.5f) * 1.12f;
                vec.Y = Utils.Remap(vec.Y, 0, 1, -0.4f, 1.1f);
                Color clr = rand.NextBool() ? ParticleClr1 : ParticleClr2;
                Vector2 adjPos = new Vector2(X, Y) + vec * new Vector2(font.MeasureString(text).X, 20);
                spriteBatch.Draw(particle, adjPos, null, clr * alpha * 0.9f, rand.NextFloat(MathHelper.TwoPi) + Main.GlobalTimeWrappedHourly * 5f * (rand.NextBool() ? 1 : -1), particle.Size() / 2f, baseScale * new Vector2(0.05f, 0.05f) * rand.NextFloat(0.8f, 1.25f), SpriteEffects.None, 0);
            }
            spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);

        }
        public static float MaxY = 0;
        public static Color TextColor = new Color(160, 106, 150);
        public static Color ParticleClr1 = new Color(255, 255, 120);
        public static Color ParticleClr2 = new Color(90, 255, 225);
        public static void Draw(Item Item, DrawableTooltipLine line) {
            Draw(Item, Main.spriteBatch, line.Text, line.X, line.Y, 0, line.BaseScale, Main.GlobalTimeWrappedHourly, FontAssets.MouseText.Value);
        }
    }
}
