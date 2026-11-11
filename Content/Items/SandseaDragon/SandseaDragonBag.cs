using CalamityEntropy.Content.Items.SandseaDragon.Loot;
using CalamityEntropy.Content.Items.Weapons.SandseaDragon.Bow;
using CalamityEntropy.Content.Items.Weapons.SandseaDragon.Conqueror;
using CalamityEntropy.Content.Items.Weapons.SandseaDragon.Sharkbite;
using CalamityEntropy.Content.Items.Weapons.SandseaDragon.SharkSquad;
using CalamityEntropy.Content.Items.Weapons.SandseaDragon.SweetRain;
using CalamityEntropy.Content.NPCs.SandseaDragon;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon
{
    /// <summary>宝藏袋(沙海冥龙):钱币、材料、至少一把武器、必出的专家饰品,面具 15%</summary>
    public class SandseaDragonBag : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.BossBag[Item.type] = true;
        }

        public override void SetDefaults() {
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.width = 46;
            Item.height = 34;
            Item.expert = true;
            Item.rare = ItemRarityID.Blue;
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
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<SandseaDragonHead>()));
            foreach (IItemDropRule rule in SandseaDragonLoot.Materials()) {
                itemLoot.Add(rule);
            }
            itemLoot.Add(SandseaDragonLoot.Weapons());
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DesertGladiatorNecklace>()));
            itemLoot.Add(SandseaDragonLoot.Mask());
        }
    }

    /// <summary>宝藏袋与普通模式的本体掉落共用一套规则,每次调用都新建实例,两张掉落表不共用对象</summary>
    internal static class SandseaDragonLoot
    {
        /// <summary>武器的掉率,分子比分母:40%</summary>
        private const int WeaponNumerator = 2;
        private const int WeaponDenominator = 5;
        /// <summary>面具 15%</summary>
        private const int MaskNumerator = 3;
        private const int MaskDenominator = 20;

        public static IItemDropRule[] Materials() {
            return new IItemDropRule[]
            {
                ItemDropRule.Common(ItemID.AncientBattleArmorMaterial, 1, 1, 3),
                ItemDropRule.Common(ItemID.LightShard, 1, 1, 3),
                ItemDropRule.Common(ItemID.DarkShard, 1, 1, 3),
                ItemDropRule.Common(ModContent.ItemType<RunicStone>(), 1, 10, 15),
                ItemDropRule.Common(ItemID.SharkFin, 1, 1, 2)
            };
        }

        /// <summary>五把武器各自 40%,一把都没出就整组重掷,所以至少出一把</summary>
        public static IItemDropRule Weapons() {
            return new AlwaysAtleastOneSuccessDropRule(
                Weapon(ModContent.ItemType<SandseaConqueror>()),
                Weapon(ModContent.ItemType<SandstormBow>()),
                Weapon(ModContent.ItemType<SharkSquadStaff>()),
                Weapon(ModContent.ItemType<SweetRainStaff>()),
                Weapon(ModContent.ItemType<AncientSharkbiteStaff>()));
        }

        public static IItemDropRule Mask() => new CommonDrop(ModContent.ItemType<SandseaDragonMask>(), MaskDenominator, 1, 1, MaskNumerator);

        private static IItemDropRule Weapon(int type) => new CommonDrop(type, WeaponDenominator, 1, 1, WeaponNumerator);
    }
}
