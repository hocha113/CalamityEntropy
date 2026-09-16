using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Miracle : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.Entropy().light += 0.4f;
            player.GetDamage(DamageClass.Generic) += 0.02f;
        }
        public override float AddDefense() {
            return 0.05f;
        }
        public override int getRollChance() {
            return 1;
        }
        public override Color getColor() {
            return Color.Violet;
        }
        public override bool Dramatic() {
            return true;
        }
    }
}
