using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class WaterWalker : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.waterWalk = true;
        }
        public override int getRollChance() {
            return 5;
        }
        public override bool? canApplyTo(Item item) {
            if (item.legSlot == -1) {
                return false;
            }
            return base.canApplyTo(item);
        }
    }
}
