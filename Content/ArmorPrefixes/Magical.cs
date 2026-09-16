using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Magical : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.Entropy().ManaCost -= 0.05f;
            player.statManaMax2 += 10;
            player.Entropy().enhancedMana += 0.06f;
        }
        public override Color getColor() {
            return Color.LightBlue;
        }
        public override int getRollChance() {
            return 1;
        }
        public override bool Dramatic() {
            return true;
        }
    }
}
