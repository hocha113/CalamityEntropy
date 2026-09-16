using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Shining : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.Entropy().light += 0.6f;
        }
        public override int getRollChance() {
            return 10;
        }
        public override bool? canApplyTo(Item item) {
            if (item.headSlot == -1) {
                return false;
            }
            return base.canApplyTo(item);
        }
    }
}
