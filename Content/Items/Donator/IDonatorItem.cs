using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public interface IDonatorItem
    {
        public string DonatorName { get; }
    }
    public class DonatorGItem : GlobalItem
    {
        // 原灾厄 donorItem 标记仅服务灾厄自身的捐助词条，脱离灾厄后由下方自有词条独立承担
        public override void ModifyTooltips(Item entity, List<TooltipLine> tooltips) {
            if (entity.ModItem != null && entity.ModItem is IDonatorItem i) {
                TooltipLine tl = new TooltipLine(Mod, "EntropyDonorName", Mod.GetLocalization("Donor").Value + " " + Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(i.DonatorName)));
                tl.OverrideColor = Color.Yellow;
                tooltips.Add(tl);
            }
        }
    }
}
