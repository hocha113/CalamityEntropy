using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Great : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.GetDamage(DamageClass.Generic) += 0.02f;
        }
        public override float AddDefense() {
            return 0.08f;
        }
        public override int getRollChance() {
            return 2;
        }
        public override Color getColor() {
            return Color.Violet;
        }
    }
}
