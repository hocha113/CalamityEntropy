using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Rust : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
        }
        public override Color getColor() {
            return Color.Gray;
        }
        public override int getRollChance() {
            return 10;
        }
        public override float AddDefense() {
            return -0.1f;
        }
    }
}
