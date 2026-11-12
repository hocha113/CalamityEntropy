using CalamityEntropy.Content.Items.Weapons.BossLoot.Moonrise;
using CalamityEntropy.Content.Items.Weapons.BossLoot.StarryNight;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SunMoonGods
{
    /// <summary>星月贤者宝藏袋,WraithSoulEssence 与 PowerOfStarmoon 由首领按人掉落,不进袋</summary>
    public class StarmoonSageBag : ModItem
    {
        //两件武器各自独立判定的掉落概率
        private const int WeaponChanceNumerator = 2;
        private const int WeaponChanceDenominator = 3;
        //旅途研究数、碰撞箱边长(像素)、世界里朝白色提亮的比例
        private const int ResearchCount = 3;
        private const int HitboxSize = 24;
        private const float WorldBrighten = 0.4f;

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = ResearchCount;
            ItemID.Sets.BossBag[Type] = true;
        }

        public override void SetDefaults() {
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.width = HitboxSize;
            Item.height = HitboxSize;
            Item.expert = true;
            Item.rare = ItemRarityID.Red;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
        }

        public override bool CanRightClick() => true;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, WorldBrighten);

        public override void PostUpdate() {
            CEUtils.ForceItemIntoWorld(Item);
            Item.TreasureBagLightAndDust();
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
            return CEUtils.DrawTreasureBagInWorld(Item, spriteBatch, ref rotation, ref scale, whoAmI);
        }

        public override void ModifyItemLoot(ItemLoot itemLoot) {
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<StarmoonSage>()));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<Moonrise>(), WeaponChanceDenominator, chanceNumerator: WeaponChanceNumerator));
            itemLoot.Add(new CommonDrop(ModContent.ItemType<StarryNight>(), WeaponChanceDenominator, chanceNumerator: WeaponChanceNumerator));
        }
    }
}
