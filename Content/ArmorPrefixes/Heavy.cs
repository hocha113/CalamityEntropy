using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Heavy : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.Entropy().moveSpeed -= 0.1f;
        }
        public override Color getColor() {
            return Color.Gray;
        }
        public override int getRollChance() {
            return 10;
        }
    }
}
