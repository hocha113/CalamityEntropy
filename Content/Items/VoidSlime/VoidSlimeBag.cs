using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.VoidSlime
{
    public class VoidSlimeBag : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.BossBag[Item.type] = true;
        }

        public override void SetDefaults() {
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.width = 36;
            Item.height = 36;
            Item.expert = true;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
        }

        public override bool CanRightClick() => true;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.4f);

        public override void PostUpdate() {
            CEUtils.ForceItemIntoWorld(Item);
            Item.TreasureBagLightAndDust();
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
            return CEUtils.DrawTreasureBagInWorld(Item, spriteBatch, ref rotation, ref scale, whoAmI);
        }

        public override void ModifyItemLoot(ItemLoot itemLoot) {
            foreach (IItemDropRule rule in SharedLoot()) {
                itemLoot.Add(rule);
            }
        }

        /// <summary>宝藏袋和普通模式本体共用的掉落,每次调用都返回一组新规则</summary>
        public static List<IItemDropRule> SharedLoot() {
            return new List<IItemDropRule> {
                ItemDropRule.Common(ItemID.PlatinumCoin, 1, 10, 10),
                new VoidSlimeWeaponDropRule(
                    ModContent.ItemType<GelstarBlade>(), ModContent.ItemType<OrbitBow>(),
                    ModContent.ItemType<PulsarStaff>(), ModContent.ItemType<SingularityRod>()),
                ItemDropRule.Common(ModContent.ItemType<SupernovaGel>(), 1, 60, 75),
                ItemDropRule.Common(ModContent.ItemType<GalaxySolidifier>()),
                ItemDropRule.Common(ModContent.ItemType<BabyStarGel>(), 4),
                ItemDropRule.Common(ModContent.ItemType<VoidSlimeSaddle>(), 4),
            };
        }
    }

    /// <summary>武器池:每把各自按 40% 掷骰,不足两把时从没中的里补到两把</summary>
    public class VoidSlimeWeaponDropRule : IItemDropRule
    {
        public const float Chance = 0.4f;
        public const int Minimum = 2;

        private readonly int[] options;

        public List<IItemDropRuleChainAttempt> ChainedRules { get; } = new List<IItemDropRuleChainAttempt>();

        public VoidSlimeWeaponDropRule(params int[] options) {
            this.options = options;
        }

        public bool CanDrop(DropAttemptInfo info) => true;

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
            List<int> picked = new List<int>();
            List<int> rest = new List<int>();
            foreach (int item in options) {
                (info.rng.NextFloat() < Chance ? picked : rest).Add(item);
            }
            while (picked.Count < Minimum && rest.Count > 0) {
                int index = info.rng.Next(rest.Count);
                picked.Add(rest[index]);
                rest.RemoveAt(index);
            }
            foreach (int item in picked) {
                CommonCode.DropItem(info, item, 1);
            }
            return new ItemDropAttemptResult { State = ItemDropAttemptResultState.Success };
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
            float rate = SingleItemRate(options.Length) * ratesInfo.parentDroprateChance;
            foreach (int item in options) {
                drops.Add(new DropRateInfo(item, 1, 1, rate, ratesInfo.conditions));
            }
            Chains.ReportDroprates(ChainedRules, 1f, drops, ratesInfo);
        }

        /// <summary>单把武器的实际掉率:自己中,或没中但被补位抽到</summary>
        private static float SingleItemRate(int count) {
            if (count <= Minimum) {
                return 1f;
            }
            float miss = 1f - Chance;
            float rate = Chance;
            //其余 count-1 把里中了 k 把,k 不足 Minimum 时还要补 Minimum-k 把,候选是 count-k 把
            for (int k = 0; k < Minimum; k++) {
                float others = Binomial(count - 1, k) * System.MathF.Pow(Chance, k) * System.MathF.Pow(miss, count - 1 - k);
                rate += miss * others * (Minimum - k) / (count - k);
            }
            return rate;
        }

        private static float Binomial(int n, int k) {
            float result = 1f;
            for (int i = 1; i <= k; i++) {
                result *= (n - k + i) / (float)i;
            }
            return result;
        }
    }
}
