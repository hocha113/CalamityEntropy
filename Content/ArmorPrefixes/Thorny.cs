using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Thorny : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.Entropy().Thorn += 1f;
        }
        public override int getRollChance() {
            return 5;
        }
        public override Color getColor() {
            return Color.Violet;
        }
    }
}
