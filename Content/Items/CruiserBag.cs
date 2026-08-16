using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.Common.EGlobalItem;

namespace CalamityEntropy.Content.Items
{
    public class CruiserBag : ModItem
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
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
        }

        public override bool CanRightClick() => true;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.4f);

        public override void PostUpdate()
        {
            CalamityMod.CalamityUtils.ForceItemIntoWorld(Item);
            Item.TreasureBagLightAndDust();
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return CalamityUtils.DrawTreasureBagInWorld(Item, spriteBatch, ref rotation, ref scale, whoAmI);
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<CruiserHead>()));

            itemLoot.Add(ModContent.ItemType<BottledFissure>(), new Fraction(3, 5));
            itemLoot.Add(ModContent.ItemType<VoidRelics>(), new Fraction(3, 5));
            itemLoot.Add(ModContent.ItemType<VoidElytra>(), new Fraction(4, 5));
            itemLoot.Add(ModContent.ItemType<VoidEcho>(), new Fraction(3, 5));
            itemLoot.Add(ModContent.ItemType<Silence>(), new Fraction(3, 5));
            itemLoot.Add(ModContent.ItemType<WingsOfHush>(), new Fraction(3, 5));
            itemLoot.Add(ModContent.ItemType<VoidAnnihilate>(), new Fraction(3, 5));
            itemLoot.Add(ModContent.ItemType<VoidCandle>(), new Fraction(2, 5));
            itemLoot.Add(ModContent.ItemType<WindOfUndertaker>(), new Fraction(3, 5));
            itemLoot.Add(ModContent.ItemType<VoidToy>(), new Fraction(1, 5));
            itemLoot.Add(ModContent.ItemType<CruiserPlush>(), new Fraction(1, 6));
            itemLoot.Add(ModContent.ItemType<VoidScales>(), new Fraction(1, 1), 40, 60);
            itemLoot.Add(ModContent.ItemType<PhantomBottle>(), new Fraction(1, 4));
            itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<TheocracyPearlToy>(), 5));

            itemLoot.Add(ModContent.ItemType<VoidMonolith>(), new Fraction(2, 5));
        }
    }
}
