using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class TheSoul : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.Entropy().LifeStealP += 0.002f;
        }
        public override Color getColor() {
            return Color.AliceBlue;
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
