using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Guarded : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {

        }
        public override float AddDefense() {
            return 0.15f;
        }
        public override int getRollChance() {
            return 3;
        }
        public override Color getColor() {
            return Color.Orange;
        }
    }
}
