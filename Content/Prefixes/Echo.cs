using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Prefixes
{
    //脱离灾厄:原继承灾厄RogueWeaponPrefix,按补充裁定改原生ModPrefix通用武器前缀
    public class Echo : ModPrefix
    {
        public override string LocalizationCategory => "Prefixes.Weapon";
        public override PrefixCategory Category => PrefixCategory.AnyWeapon;
        public override bool CanRoll(Item item) => item.maxStack == 1 || item.AllowReforgeForStackableItem;
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item) {
            TooltipLine t = new TooltipLine(Mod, "PrefixDescription", AdditionalTooltip.Value) {
                IsModifier = true,
                IsModifierBad = false
            };
            yield return t;
        }
        public LocalizedText AdditionalTooltip => Language.GetOrRegister(Mod.GetLocalizationKey("Prefix" + this.Name + "Descr"));

    }
}
