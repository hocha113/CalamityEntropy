using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.Rarities
{
    public class AzafureOrange : ModRarity
    {
        public override Color RarityColor => Color.OrangeRed;

        public override int GetPrefixedRarity(int offset, float valueMult) => Type;
        public static void Draw(Item Item, SpriteBatch spriteBatch, string text, int X, int Y, float rotation,
            Vector2 baseScale, float time, DynamicSpriteFont font) {
            Texture2D glow = CEExtraAssets.Glow;
            Texture2D particle = CEExtraAssets.Smoke;
            spriteBatch.UseBlendState_UI(BlendState.Additive);
            Vector2 origin = font.MeasureString(text) * new Vector2(1, 0.6f) * 0.5f;
            float ey = CELists.tooltipNameUpList.Contains(Language.ActiveCulture.Name) ? 0 : 4;
            spriteBatch.Draw(glow, new Vector2(X, Y + ey) + origin, null, Color.DeepSkyBlue * 0.6f, 0, glow.Size() * 0.5f, origin * 0.017f * new Vector2(1, 1f), SpriteEffects.None, 0);

            UnifiedRandom rand = new UnifiedRandom((int)(Item.type.GetHashCode()));
            int particleCount = (int)(font.MeasureString(text).X * 0.08f);

            for (int i = 0; i < particleCount; i++) {
                Vector2 vec = new Vector2(rand.NextFloat(), rand.NextFloat());
                vec.Y = CEUtils.Frac(vec.Y - time * 1f);
                float alpha = 1;
                if (vec.Y < 0.25f)
                    alpha = vec.Y / 0.25f;
                if (vec.Y > 0.75f)
                    alpha = (1 - vec.Y) / 0.25f;
                vec.X = 0.5f + (vec.X - 0.5f) * 1.1f;
                vec.Y = 0.5f + (vec.Y - 0.5f) * 1.12f;
                vec.Y = Utils.Remap(vec.Y, 0, 1, -0.4f, 1.1f);
                Color clr = ParticleColor * 0.76f;
                Vector2 adjPos = new Vector2(X, Y) + vec * new Vector2(font.MeasureString(text).X, 20);
                for (int ii = 0; ii < 1; ii++) {
                    Color c = clr * alpha * 0.9f;
                    spriteBatch.Draw(particle, adjPos, null, c, rand.NextFloat(MathHelper.TwoPi) + Main.GlobalTimeWrappedHourly * 5f * (rand.NextBool() ? 1 : -1), particle.Size() / 2f, baseScale * new Vector2(0.042f, 0.042f) * rand.NextFloat(0.8f, 1.25f), SpriteEffects.None, 0);
                }
            }
            spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);

            for (float j = 0; j < MathHelper.TwoPi; j += MathHelper.PiOver4 * 0.5f) {
                spriteBatch.DrawString(font, text, new Vector2(X, Y) + j.ToRotationVector2() * 1.5f, (TextColor * 0.2f));
            }
            spriteBatch.DrawString(font, text, new Vector2(X, Y), TextColor * 1.8f);
            spriteBatch.UseBlendState_UI(BlendState.Additive);
            for (int i = 0; i < particleCount / 2; i++) {
                Vector2 vec = new Vector2(rand.NextFloat(), rand.NextFloat());
                vec.Y = CEUtils.Frac(vec.Y - time * 1f);
                float alpha = 1;
                if (vec.Y < 0.25f)
                    alpha = vec.Y / 0.25f;
                if (vec.Y > 0.75f)
                    alpha = (1 - vec.Y) / 0.25f;
                vec.X = 0.5f + (vec.X - 0.5f) * 1.1f;
                vec.Y = 0.5f + (vec.Y - 0.5f) * 1.12f;
                vec.Y = Utils.Remap(vec.Y, 0, 1, -0.4f, 1.1f);
                ParticleColor = new Color(130, 70, 0);
                Color clr = ParticleColor;
                Vector2 adjPos = new Vector2(X, Y) + vec * new Vector2(font.MeasureString(text).X, 20);
                for (int ii = 0; ii < 1; ii++) {
                    Color c = clr * alpha * 0.4f;
                    spriteBatch.Draw(particle, adjPos, null, c, rand.NextFloat(MathHelper.TwoPi) + Main.GlobalTimeWrappedHourly * 5f * (rand.NextBool() ? 1 : -1), particle.Size() / 2f, baseScale * new Vector2(0.034f, 0.034f) * rand.NextFloat(0.8f, 1.25f), SpriteEffects.None, 0);
                }
            }
            spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
        }
        public static Color TextColor = new Color(204, 71, 35);
        public static Color ParticleColor = new Color(130, 70, 0);
        public static void Draw(Item Item, DrawableTooltipLine line) {
            Draw(Item, Main.spriteBatch, line.Text, line.X, line.Y, 0, line.BaseScale, Main.GlobalTimeWrappedHourly, FontAssets.MouseText.Value);
        }
    }
}
