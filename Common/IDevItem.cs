using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public interface IDevItem
    {
        public string DevName { get; }
    }
    public class DevGItem : GlobalItem
    {
        // 原借助灾厄 devItem 旗标获得的开发者物品彩虹光效已随灾厄脱钩移除，仅保留归属者提示行

        public override void ModifyTooltips(Item entity, List<TooltipLine> tooltips) {
            if (entity.ModItem != null && entity.ModItem is IDevItem i) {
                TooltipLine tl = new TooltipLine(Mod, "EntropyDonorName", Mod.GetLocalization("Owner").Value + " " + i.DevName);
                tl.OverrideColor = Color.Yellow;
                tooltips.Add(tl);
            }
        }
    }
}
