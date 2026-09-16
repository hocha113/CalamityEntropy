using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Void : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.Entropy().shootSpeed += 0.5f;
            player.Entropy().AttackVoidTouch += 0.01f;
            player.Entropy().Thorn += 0.2f;
        }
        public override int getRollChance() {
            return 3;
        }
        public override bool? canApplyTo(Item item) {
            if (!Main.hardMode) return false;
            return base.canApplyTo(item);
        }
    }
}
