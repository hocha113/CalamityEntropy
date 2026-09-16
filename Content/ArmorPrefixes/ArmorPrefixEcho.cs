using CalamityEntropy.Core.Weapons;
using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class ArmorPrefixEcho : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item) {
            //脱离灾厄:原+4%潜行回复,按rogue-weapons通用换算(x1.5)改为大招充能速度+6%
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.06f;
        }
        public override Color getColor() {
            return Color.DarkBlue;
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
