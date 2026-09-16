using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.Common.EGlobalItem;

namespace CalamityEntropy.Content.Items
{
    public class CruiserBag : ModItem
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
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<CruiserHead>()));

            //脱离灾厄:原灾厄DropHelper.Add扩展换原版规则,分数概率按CommonDrop(物品,分母,最少,最多,分子)对位
            itemLoot.Add(new CommonDrop(ModContent.ItemType<BottledFissure>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<VoidRelics>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<VoidElytra>(), 5, 1, 1, 4));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<VoidEcho>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<Silence>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<WingsOfHush>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<VoidAnnihilate>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<VoidCandle>(), 5, 1, 1, 2));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<WindOfUndertaker>(), 5, 1, 1, 3));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidToy>(), 5));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CruiserPlush>(), 6));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidScales>(), 1, 40, 60));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PhantomBottle>(), 4));
            itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<TheocracyPearlToy>(), 5));

            itemLoot.Add(new CommonDrop(ModContent.ItemType<VoidMonolith>(), 5, 1, 1, 2));

            // 渊海灾虫宠物重挂（bookmark-rehang 增补段）；书签/武器类重挂由 EGlobalItem 统一注入，此处勿重复
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<AquaticFlute>(), 4));
        }
    }
}
