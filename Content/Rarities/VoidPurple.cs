using CalamityEntropy.Core.CalamityRef;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Rarities
{
    public class VoidPurple : ModRarity
    {
        public override Color RarityColor => new Color(106, 40, 190);

        // 脱离灾厄:前后缀降档原指向灾厄 CosmicPurple/BurnishedAuric,按 rarity-map 换自有档
        public override int GetPrefixedRarity(int offset, float valueMult) => offset switch
        {
            -2 => CECal.RarityCosmicPurple(ModContent.RarityType<AbyssalBlue>()),
            -1 => CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>()),
            1 => ModContent.RarityType<VoidPurple>(),
            2 => ModContent.RarityType<VoidPurple>(),
            _ => Type,
        };
    }
}
