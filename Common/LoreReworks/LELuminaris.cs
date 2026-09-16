using CalamityEntropy.Content.Items.Lores;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.LoreReworks
{
    public class LELuminaris : LoreEffect
    {
        public override int ItemType => ModContent.ItemType<LuminarisLore>();
        public override void UpdateEffects(Player player) {
            player.Entropy().WingTimeMult += LuminarisLore.wingTimeAddition;
        }
        public override void ModifyTooltip(TooltipLine tooltip) {
            tooltip.Text = tooltip.Text.Replace("{1}", LuminarisLore.wingTimeAddition.ToPercent().ToString());
        }
    }
}
