using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.Wyrm;
using CalamityEntropy.Content.Items.Donator;
using CalamityMod;
using CalamityMod.ChatTags;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityEntropy.Core.ChatTags
{
    public sealed class TextEffectHandler : AbstractTagHandler<TextEffectHandler>
    {
        protected override string[] TagNames { get; } = ["ceeffect"];

        public override TextSnippet Parse(string text, Color baseColor = new(), string options = null)
        {
            if (!CalamityClientConfig.Instance.TextEffects)
                return new TextSnippet(text);
            if (options.Equals("cruiser", StringComparison.OrdinalIgnoreCase))
                return new CruiserSnippet(text);
            return new TextSnippet(text);
        }
    }
    public sealed class CruiserSnippet(string text) : TextSnippet
    {
        public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = new Vector2(), Color color = new Color(), float scale = 1)
        {
            size = new Vector2(GetStringLength(FontAssets.MouseText.Value), FontAssets.MouseText.Value.MeasureString(" ").Y * scale);
            if (position == Vector2.Zero)
                return true;
            Vector2 line = position + new Vector2(2 * Scale, 0);
            var font = FontAssets.MouseText.Value;
            Vector2 origin = font.MeasureString(text) * new Vector2(1, 0.6f) * 0.5f;
            float xa = 0;
            List<float> scales = new List<float>() { 0, 0.5f };
            Vector2 ms = font.MeasureString(text);
            ms.Y *= 0.7f;
            for (int i_ = 0; i_ < scales.Count; i_++)
            {
                scales[i_] = CEUtils.Frac(scales[i_] + Main.GlobalTimeWrappedHourly);
                float sc = scales[i_] * 12f;
                Main.spriteBatch.DrawString(font, text, new Vector2(-sc, 0) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180) * 0.4f, scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);
                Main.spriteBatch.DrawString(font, text, new Vector2(sc, 0) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180) * 0.4f, scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);
                Main.spriteBatch.DrawString(font, text, new Vector2(0, sc) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180) * 0.4f, scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);
                Main.spriteBatch.DrawString(font, text, new Vector2(0, -sc) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180) * 0.4f, scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);
            }
            for (int i = 0; i < text.Length; i++)
            {
                string chr = text[i].ToString();
                Vector2 sizez = font.MeasureString(chr);
                float yofs;
                float lerp = 0.5f + (0.5f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * -6 + i * 3f / text.Length)));
                Color colord = Color.Lerp(Color.Black, new Color(20, 16, 250), lerp);
                Color strokeColord = new Color(160, 100, 255);
                yofs = 0;

                float sof = 0.4f;
                for (float ir = 0; ir < MathHelper.TwoPi; ir += MathHelper.PiOver4)
                {
                    Main.spriteBatch.DrawString(font, chr, new Vector2(line.X + xa, line.Y + yofs) + ir.ToRotationVector2() * sof, strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                }
                Main.spriteBatch.DrawString(font, chr, new Vector2(line.X + xa, line.Y + yofs), colord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                xa += sizez.X;
            }
            return true;
        }
        public override float GetStringLength(DynamicSpriteFont font)
        {
            float size = font.MeasureString(text).X + 4;
            return size * Scale;
        }
    }
}
