using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Silence : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            // 潜行体系退役:原+2%潜行上限按 Echo 前缀先例减半转通用伤害
            player.GetDamage(DamageClass.Generic) += 0.01f;
        }
        public override Color getColor() {
            return Color.Black;
        }
        public override int getRollChance() {
            return 4;
        }

        public override bool Precious() {
            return true;
        }
    }
}
