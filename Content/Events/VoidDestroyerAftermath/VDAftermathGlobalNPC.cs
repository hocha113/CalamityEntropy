using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.Items.Weapons.Aftermath.AncientPaladinWhip;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Core.CalamityRef;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Events.VoidDestroyerAftermath
{
    public class VDAftermathGlobalNPC : GlobalNPC
    {
        /// <summary>两端都在 SetDefaults 里按世界旗标查表,联机下客户端算出的血量与服务端一致</summary>
        public override void SetDefaults(NPC npc) {
            if (!VDAftermath.Active || !VDAftermath.StatBoosts.TryGetValue(npc.type, out VDStatBoost boost)) {
                return;
            }
            npc.lifeMax = (int)(npc.lifeMax * boost.Life);
            npc.damage = (int)(npc.damage * boost.Damage);
            npc.defense = (int)(npc.defense * boost.Defense);
            if (boost.KnockBackResist >= 0f) {
                npc.knockBackResist = boost.KnockBackResist;
            }
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot) {
            if (VDAftermath.SunlightDrops.TryGetValue(npc.type, out VDMaterialDrop sun)) {
                npcLoot.Add(PostRule(VDAftermath.SunlightType, sun.Chance, sun.Min, sun.Max));
            }
            if (VDAftermath.StarSpiritDrops.TryGetValue(npc.type, out VDMaterialDrop star)) {
                npcLoot.Add(PostRule(VDAftermath.StarSpiritType, star.Chance, star.Min, star.Max));
            }

            switch (npc.type) {
                case NPCID.PigronHallow:
                    ReplaceVanilla(npcLoot, ItemID.Bacon, 4);
                    ReplaceVanilla(npcLoot, ItemID.HamBat, 10);
                    break;
                case NPCID.ChaosElemental:
                    ReplaceVanilla(npcLoot, ItemID.ApplePie, 5);
                    ReplaceVanilla(npcLoot, ItemID.RodofDiscord, 100);
                    break;
                case NPCID.BigMimicHallow:
                    GateVanilla(npcLoot, ItemID.GreaterHealingPotion);
                    GateVanilla(npcLoot, ItemID.GreaterManaPotion);
                    npcLoot.Add(PostRule(ModContent.ItemType<VoidHealingPotion>(), 1, 5, 10));
                    npcLoot.Add(PostRule(ModContent.ItemType<VoidManaPotion>(), 1, 5, 15));
                    npcLoot.Add(PostRule(ModContent.ItemType<Vitalfeather>(), 100, 1, 1, 33));
                    //装灾厄时圣火流石仍由亵渎天神出,这里加门免得双来源
                    if (!CERef.Has) {
                        npcLoot.Add(PostRule(ModContent.ItemType<SacredStone>(), 3, 1, 1));
                    }
                    break;
                case NPCID.DarkCaster:
                    npcLoot.Add(PostRule(ItemID.WaterBolt, 30, 1, 1));
                    break;
                case NPCID.Necromancer:
                case NPCID.NecromancerArmored:
                    ReplaceVanilla(npcLoot, ItemID.ShadowbeamStaff, 20, 3);
                    break;
                case NPCID.RaggedCaster:
                case NPCID.RaggedCasterOpenCoat:
                    ReplaceVanilla(npcLoot, ItemID.SpectreStaff, 20, 3);
                    break;
                case NPCID.DiabolistRed:
                case NPCID.DiabolistWhite:
                    ReplaceVanilla(npcLoot, ItemID.InfernoFork, 20, 3);
                    break;
                case NPCID.Paladin:
                    ReplaceVanilla(npcLoot, ItemID.PaladinsHammer, 3, 2);
                    ReplaceVanilla(npcLoot, ItemID.PaladinsShield, 3, 2);
                    npcLoot.Add(PostRule(ModContent.ItemType<AncientPaladinWhip>(), 4, 1, 1));
                    break;
            }
        }

        private static IItemDropRule PostRule(int itemId, int chanceDenominator, int min, int max, int chanceNumerator = 1) {
            return new ItemDropWithConditionRule(itemId, chanceDenominator, min, max, new PostVoidDestroyerCondition(), chanceNumerator);
        }

        /// <summary>原版规则改挂到驱逐舰前,驱逐舰后换成新掉率</summary>
        private static void ReplaceVanilla(NPCLoot npcLoot, int itemId, int chanceDenominator, int chanceNumerator = 1) {
            GateVanilla(npcLoot, itemId);
            npcLoot.Add(PostRule(itemId, chanceDenominator, 1, 1, chanceNumerator));
        }

        private static void GateVanilla(NPCLoot npcLoot, int itemId) {
            List<IItemDropRule> rules = npcLoot.Get(false).Where(rule => DropsOnly(rule, itemId)).ToList();
            foreach (IItemDropRule rule in rules) {
                npcLoot.Remove(rule);
                LeadingConditionRule gate = new(new PreVoidDestroyerCondition());
                gate.OnSuccess(rule);
                npcLoot.Add(gate);
            }
        }

        /// <summary>规则报告的掉落全是这一种物品才算,混着别的物品的规则不动</summary>
        private static bool DropsOnly(IItemDropRule rule, int itemId) {
            List<DropRateInfo> rates = new();
            rule.ReportDroprates(rates, new DropRateInfoChainFeed(1f));
            return rates.Count > 0 && rates.TrueForAll(info => info.itemId == itemId);
        }
    }
}
