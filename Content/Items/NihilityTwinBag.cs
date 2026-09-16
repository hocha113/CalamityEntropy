using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class NihilityTwinBag : ModItem
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
            Item.rare = ModContent.RarityType<NihilityBlue>();
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
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<NihilityActeriophage>()));

            //脱离灾厄:原灾厄DropHelper.Add扩展换原版规则,分数概率按CommonDrop(物品,分母,最少,最多,分子)对位
            itemLoot.Add(new CommonDrop(ModContent.ItemType<NihilityShell>(), 5, 1, 1, 4));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<Voidseeker>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<EventideSniper>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<NihilityBacteriophageWand>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<StarlessNight>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<VoidPathology>(), 5, 1, 1, 3));
            // 虚无碎片升为月后一阶通货，按 NPC 主力静态审计加量（26,32 → 32,40）
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NihilityFragments>(), 1, 32, 40));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ChaoticPiece>(), 1, 26, 32));
            // 灾厄宝袋重挂物（BookMarkProfaned 等）由 EGlobalItem 统一注入，此处勿重复
        }
    }
}
