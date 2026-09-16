using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Hard : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {

        }
        public override float AddDefense() {
            return 0.1f;
        }
        public override int getRollChance() {
            return 10;
        }
    }
}
