using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.Apsychos;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class ApsychosBag : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.BossBag[Type] = true;
            ItemID.Sets.PreHardmodeLikeBossBag[Type] = true;
        }

        public override void SetDefaults() {
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.width = 32;
            Item.height = 32;
            Item.expert = true;
            Item.rare = ItemRarityID.LightRed;
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
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Apsychos>()));

            //脱离灾厄:原灾厄DropHelper.Add扩展换原版规则,分数概率按CommonDrop(物品,分母,最少,最多,分子)对位
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TectonicShard>(), 1, 36, 42));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<GreatSwordofEmbers>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<AshesCore>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<ScorchingChakram>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<AshesBow>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<EmberBolt>(), 5, 1, 1, 3));
            itemLoot.Add(ItemDropRule.Common(ItemID.Hellstone, 1, 52, 64));
        }
    }
}
