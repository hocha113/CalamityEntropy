using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Potions;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    public partial class CruiserHead
    {
        #region 图鉴头像
        public static int icon = ModContent.GetModBossHeadSlot("CalamityEntropy/Content/NPCs/Cruiser/CruiserHead_Head_Boss");
        public static int iconP2;
        public static void loadHead() {
            string path = "CalamityEntropy/Content/NPCs/Cruiser/p2head";
            CalamityEntropy.Instance.AddBossHeadTexture(path, -1);
            iconP2 = ModContent.GetModBossHeadSlot(path);
        }
        public override void BossHeadSlot(ref int index) {
            if (phaseTrans >= CruiserDirector.PhaseTransDrawSwitch) {
                index = iconP2;
            }
            else {
                index = icon;
            }
        }
        public override void BossHeadRotation(ref float rotation) {
            rotation = NPC.rotation - MathHelper.PiOver2;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.CruiserBestiary")
            });
        }
        #endregion

        #region 掉落
        public override void BossLoot(ref int potionType) {
            potionType = ModContent.ItemType<VoidHealingPotion>();
        }

        public override void OnKill() {
            if (!EDownedBosses.downedCruiser) {
                VoidOreSystem.BlessWorldWithOre();
            }

            NPC.SetEventFlagCleared(ref EDownedBosses.downedCruiser, -1);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<CruiserBag>()));

            // 月后虚空治疗药水,数量沿用原欧米茄档 8-23;隐藏图鉴条目
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<VoidHealingPotion>(), 1, 8, 23, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            {
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<BottledFissure>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<VoidRelics>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<VoidElytra>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<VoidEcho>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<Silence>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<VoidAnnihilate>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<WindOfUndertaker>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<WingsOfHush>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<VoidCandle>(), 5, 1, 1, 3));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<VoidMonolith>(), 3));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<VoidToy>(), 3));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<VoidScales>(), 1, 88, 128));
            }
            npcLoot.Add(normalOnly);
            // 遗物:原灾厄复仇/大师条件对齐原版大师掉落惯例(difficulty-map)
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<CruiserRelic>()));

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CruiserTrophy>(), 10));

            // 首杀传记:承接原灾厄按人实例掉落语义
            // 龙牙 65-80 与 BookmarkMarivium 的原始飞龙职能承接已在 EGlobalNPC 统一登记,此处不重复
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<CruiserLore>(), 1, 1, 1, new LoreFirstKill()));
        }

        // 恒真但隐藏图鉴条目的条件:对应原 hideLootReport 语义
        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        // 首杀传记条件:对应 downed 旗标未置位时每名玩家各掉一份
        private class LoreFirstKill : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedCruiser;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        #endregion
    }
}
