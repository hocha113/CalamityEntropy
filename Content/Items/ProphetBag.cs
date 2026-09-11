using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.CrystalBalls;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs.Prophet;
using CalamityEntropy.Core.CalamityRef;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class ProphetBag : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.BossBag[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.width = 24;
            Item.height = 24;
            Item.expert = true;
            Item.rare = ItemRarityID.Yellow;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
        }

        public override bool CanRightClick() => true;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.4f);

        public override void PostUpdate()
        {
            CEUtils.ForceItemIntoWorld(Item);
            Item.TreasureBagLightAndDust();
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return CEUtils.DrawTreasureBagInWorld(Item, spriteBatch, ref rotation, ref scale, whoAmI);
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<TheProphet>()));

            //脱离灾厄:原灾厄DropHelper.Add扩展换原版规则,分数概率按CommonDrop(物品,分母,最少,最多,分子)对位
            itemLoot.Add(new CommonDrop(ModContent.ItemType<RuneSong>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<UrnOfSouls>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<SpiritBanner>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<RuneMachineGun>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<ProphecyFlyingKnife>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<ForeseeOrb>(), 5, 1, 1, 4));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<RuneWing>(), 5, 1, 1, 4));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<ForeseeWhip>(), 5, 1, 1, 2));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<ProphecyMasterpiece>(), 5, 1, 1, 3));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<BookMarkForesee>(), 5, 1, 1, 3));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedThread>()));
            // 自灾厄白金星舰宝袋重挂（bookmark-rehang 增补段）。装灾厄时白金星舰自己会出,这里加门消除双来源
            if (!CERef.Has)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NightProjection>(), 4));
            }
        }
    }
}
