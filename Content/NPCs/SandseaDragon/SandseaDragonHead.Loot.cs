using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.SandseaDragon;
using CalamityEntropy.Content.Items.SandseaDragon.Loot;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    public partial class SandseaDragonHead
    {
        /// <summary>纪念章 10%</summary>
        private const int TrophyDenominator = 10;
        /// <summary>专属宠物 25%,不分难度</summary>
        private const int PetDenominator = 4;

        /// <summary>Boss 死亡时原版按这个类型掉 5 到 15 瓶药水</summary>
        public override void BossLoot(ref int potionType) {
            potionType = ItemID.GreaterHealingPotion;
        }

        /// <summary>
        /// 专家及以上只掉宝藏袋,材料、武器和面具都在袋里;普通模式直接掉这三样,没有专家饰品
        /// 纪念章、宠物、圣物不分普通专家,由本体掉
        /// </summary>
        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<SandseaDragonBag>()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            foreach (IItemDropRule rule in SandseaDragonLoot.Materials()) {
                normalOnly.OnSuccess(rule);
            }
            normalOnly.OnSuccess(SandseaDragonLoot.Weapons());
            normalOnly.OnSuccess(SandseaDragonLoot.Mask());
            npcLoot.Add(normalOnly);

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SandseaDragonTrophy>(), TrophyDenominator));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SandseaDragonEgg>(), PetDenominator));
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<SandseaDragonRelic>()));
        }

        public override void OnKill() {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedSandseaDragon, -1);
        }
    }
}
