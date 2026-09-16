using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Broken : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
        }
        public override Color getColor() {
            return Color.Gray;
        }
        public override int getRollChance() {
            return 5;
        }
        public override float AddDefense() {
            return -0.2f;
        }
    }
}
