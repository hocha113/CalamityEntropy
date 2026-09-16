using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    /// <summary>虚渺套装的护甲面板文案。共通段 vfb 加各职业头盔专属段。
    /// 五顶头盔原先一顶都没写 player.setBonus,面板的"套装奖励"一栏因此恒为空</summary>
    internal static class VoidFaquirSet
    {
        public static string BonusText(Mod mod, string helmKey) {
            string text = mod.GetLocalization("vfb").Value + "\n" + mod.GetLocalization(helmKey).Value;
            return text.Replace("[KEY]", Common.EModPlayer.ArmorSetBonusHotKey.TooltipKeyHint());
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class VoidFaquirBodyArmor : ModItem
    {
        public override void SetStaticDefaults() {
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false;
        }
        public override void Load() {
            if (Main.netMode != NetmodeID.Server) {
                EquipLoader.AddEquipTexture(Mod, "CalamityEntropy/Content/Items/Armor/VoidFaquir/VoidFaquirBodyArmor_Back", EquipType.Back, this);
            }
        }

        public override void SetDefaults() {
            Item.width = 38;
            Item.height = 34;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.defense = 44;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void UpdateEquip(Player player) {
            player.GetDamage(DamageClass.Generic) += 0.12f;
            player.GetCritChance(DamageClass.Generic) += 5;
        }

        public override void AddRecipes() {
            if (CECal.CalChainReady(CEID.Item_TwistingNether)) {
                CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 18)
                .AddIngredient(CEID.Item_TwistingNether, 5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 18)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
