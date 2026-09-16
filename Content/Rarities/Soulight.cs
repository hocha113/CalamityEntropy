using Terraria.ModLoader;

namespace CalamityEntropy.Content.Rarities
{
    public class Soulight : ModRarity
    {
        public override Color RarityColor => new Color(220, 240, 255);

        public override int GetPrefixedRarity(int offset, float valueMult) => offset switch {
            _ => Type,
        };
    }
}
