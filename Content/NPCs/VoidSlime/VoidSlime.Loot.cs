using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.Items.VoidSlime;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidSlime
{
    public partial class VoidSlime
    {
        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<VoidSlimeBag>()));
            //药水按人掉落并隐藏图鉴条目
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<VoidHealingPotion>(), 1, 9, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            foreach (IItemDropRule rule in VoidSlimeBag.SharedLoot()) {
                normalOnly.OnSuccess(rule);
            }
            npcLoot.Add(normalOnly);

            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<VoidSlimeRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidSlimeTrophy>(), 10));
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<VoidSlimeLore>(), 1, 1, 1, new LoreFirstKill()));
        }

        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        private class LoreFirstKill : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedVoidSlime;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
    }
}
