using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class End : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            player.GetDamage(DamageClass.Generic) += 0.05f;
            player.GetCritChance(DamageClass.Generic) += 4;
            player.GetKnockback(DamageClass.Generic) += 0.2f;
            player.Entropy().Thorn += 1f;
            player.Entropy().AttackVoidTouch += 0.01f;
        }
        public override float AddDefense() {
            return 0.12f;
        }
        public override Color getColor() {
            return Color.DarkRed;
        }
        public override int getRollChance() {
            return 1;
        }
        public override bool Dramatic() {
            return true;
        }
        public override bool? canApplyTo(Item item) {
            if (!Main.hardMode) return false;
            return Main.rand.NextBool(2);
        }
        public override bool Precious() {
            return true;
        }
    }
}
