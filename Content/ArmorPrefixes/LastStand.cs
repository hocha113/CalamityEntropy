using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using Terraria;

namespace CalamityEntropy.Content.ArmorPrefixes
{
    public class LastStand : ArmorPrefix
    {
        public override void UpdateEquip(Player player, Item item)
        {
            player.Entropy().damageReduce += 0.02f;
            player.Entropy().LastStand = true;
        }
        public override float AddDefense()
        {
            return 0.15f;
        }
        public override int getRollChance()
        {
            return 1;
        }
        public override Color getColor()
        {
            return Color.Violet;
        }
        public override bool Dramatic()
        {
            return true;
        }
        public override bool Precious()
        {
            return true;
        }
        public override bool? canApplyTo(Item item)
        {
            // 灾厄在场读终灾,缺席回落巡游者
            if (!CECal.DownedCalamitas(EDownedBosses.downedCruiser))
            {
                return false;
            }
            return Main.rand.NextBool(3);
        }
    }
}
