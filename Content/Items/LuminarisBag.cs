using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Vanity.Luminar;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.LuminarisMoth;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class LuminarisBag : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.BossBag[Item.type] = true;
        }

        public override void SetDefaults() {
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.width = 24;
            Item.height = 24;
            Item.expert = true;
            Item.rare = ModContent.RarityType<Lunarblight>();
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
        }

        public override bool CanRightClick() => true;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.6f);

        public override void PostUpdate() {
            CEUtils.ForceItemIntoWorld(Item);
            Item.TreasureBagLightAndDust();
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
            return CEUtils.DrawTreasureBagInWorld(Item, spriteBatch, ref rotation, ref scale, whoAmI);
        }

        public override void ModifyItemLoot(ItemLoot itemLoot) {
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Luminaris>()));

            //脱离灾厄:原灾厄DropHelper.Add扩展换原版规则,分数概率按CommonDrop(物品,分母,最少,最多,分子)对位
            itemLoot.Add(new CommonDrop(ModContent.ItemType<StarlitPiercer>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<Luminar>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<StarSootInjector>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<PhantomLightWing>(), 5, 1, 1, 4));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<LunarPlank>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<BottledStarlightCocoon>(), 5, 1, 1, 4));
            // 2026-08-31 平衡案:暗影披风掉落率与其他专家饰品一致(100%)
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShadeCloak>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<StarlitScaleDust>(), 1, 52, 74));
            //脱离灾厄:原灾厄DefineConditionalDropSet(1/4几率成套)换原生前置条件规则
            LeadingConditionRule vanitySet = new LeadingConditionRule(new LuminarVanityChance());
            vanitySet.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LuminarRing>()));
            vanitySet.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LuminarDress>()));
            vanitySet.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LuminarTrousers>()));
            itemLoot.Add(vanitySet);

        }

        //1/4几率放行整套时装的原生掉落条件
        private class LuminarVanityChance : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => info.rng.NextBool(4);
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
    }
}
