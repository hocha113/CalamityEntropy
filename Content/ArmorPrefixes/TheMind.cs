using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class TheMind : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.statManaMax2 += player.statManaMax2 / 5;
        }
        public override Color getColor() {
            return new Color(60, 60, 255);
        }
        public override int getRollChance() {
            return 0;
        }
        public override bool Dramatic() {
            return true;
        }
        public override bool Precious() {
            return true;
        }
    }
}
