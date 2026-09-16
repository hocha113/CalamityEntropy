using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class TheBody : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.statLifeMax2 += player.statLifeMax2 / 10;
        }
        public override Color getColor() {
            return Color.DarkRed;
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
