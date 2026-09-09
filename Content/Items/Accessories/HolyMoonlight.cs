using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class HolyMoonlight : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 86;
            Item.height = 86;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.accessory = true;

        }

        // 2026-08-31 平衡案重做:+40魔力上限与10%魔法伤害;每30秒获得魔力护盾
        // (取最大魔力50%,上限250);护盾存在时魔力病减半;护盾接触伤害与耗尽爆炸保留;
        // 护盾冷却期间按魔力吸血(100:1,45帧CD,单次上限5)。
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statManaMax2 += 40;
            player.GetDamage(DamageClass.Magic) += 0.10f;
            player.Entropy().holyMoonlight = true;
            player.Entropy().visualMagiShield = !hideVisual;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_RoverDrive, CEID.Item_ManaPolarizer, CEID.Item_CryoStone, CEID.Item_AscendantSpiritEssence))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_RoverDrive, 1).
                AddIngredient(CEID.Item_ManaPolarizer, 1).
                AddIngredient(CEID.Item_CryoStone, 1).
                AddIngredient(ModContent.ItemType<VoidBar>(), 5).
                AddIngredient(CEID.Item_AscendantSpiritEssence, 4).
                AddTile(ModContent.TileType<VoidWellTile>()).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient(ItemID.MoonStone, 1).
                AddIngredient(ItemID.ManaFlower, 1).
                AddIngredient(ModContent.ItemType<VoidBar>(), 5).
                AddTile(ModContent.TileType<VoidWellTile>()).
                Register();
        }
    }
}
