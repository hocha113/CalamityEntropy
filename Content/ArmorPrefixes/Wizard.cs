using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class Wizard : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item)
        {
            player.GetDamage(DamageClass.Generic) -= 0.10f;
            player.maxMinions += 1;
        }
        public override bool? canApplyTo(Item item)
        {
            // 灾厄在场读终灾,缺席回落巡游者
            if (!CECal.DownedCalamitas(EDownedBosses.downedCruiser))
            {
                return false;
            }
            return base.canApplyTo(item);
        }
        public override int getRollChance()
        {
            return 1;
        }
    }
}
