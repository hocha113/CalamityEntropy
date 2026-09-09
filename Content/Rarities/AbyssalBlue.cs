using CalamityEntropy.Core.CalamityRef;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Rarities
{
    public class AbyssalBlue : ModRarity
    {
        public override Color RarityColor => new Color(106, 40, 190);

        // 脱离灾厄:前后缀降档原指向灾厄 BurnishedAuric/CalamityRed,按 rarity-map 换自有档
        public override int GetPrefixedRarity(int offset, float valueMult) => offset switch
        {
            -2 => CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>()),
            -1 => CECal.RarityCalamityRed(ModContent.RarityType<VoidPurple>()),
            1 => ModContent.RarityType<AbyssalBlue>(),
            2 => ModContent.RarityType<AbyssalBlue>(),
            _ => Type,
        };
    }
}
