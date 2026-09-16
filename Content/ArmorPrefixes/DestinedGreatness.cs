using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class DestinedGreatness : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.luck += 0.1f;
            player.GetCritChance(DamageClass.Generic) += 2;
        }
        public override float AddDefense() {
            return 0.05f;
        }
        public override int getRollChance() {
            return 2;
        }
        public override Color getColor() {
            return Color.Violet;
        }
    }
}
