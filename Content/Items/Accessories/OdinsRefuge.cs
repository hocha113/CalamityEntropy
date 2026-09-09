using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Shield)]
    public class OdinsRefuge : ModItem
    {
        // 2026-08-31 平衡案重做:18防,免疫击退,免疫火块,
        // 拥有神圣屏障格挡,给自己与所有队友15%免伤(不叠加),+600仇恨。
        // 减益免疫与原版十字章护身符同一组,不含渊洋神迹那张额外表。
        public const float TeamWardDR = 0.15f;

        public override void SetDefaults()
        {
            Item.width = 86;
            Item.height = 86;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.accessory = true;
            Item.defense = 18;

        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 神圣屏障格挡
            player.Entropy().holyMantle = true;
            // 团队免伤光环(结算在 EModPlayer 的减伤汇总处,不可叠加)
            player.Entropy().odinAura = true;
            player.noKnockback = true;
            player.fireWalk = true;
            ApplyAnkhCharmImmune(player);
            player.aggro += 600;
        }

        /// <summary>与原版十字章护身符(ItemID.AnkhCharm, Player.cs type 1612)同一组减益。</summary>
        public static void ApplyAnkhCharmImmune(Player player)
        {
            player.buffImmune[BuffID.Weak] = true;
            player.buffImmune[BuffID.BrokenArmor] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Slow] = true;
            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Silenced] = true;
            player.buffImmune[BuffID.Cursed] = true;
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.Stoned] = true;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AsgardianAegis, CEID.Item_RampartofDeities))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_AsgardianAegis, 1).
                AddIngredient(CEID.Item_RampartofDeities, 1).
                AddIngredient(ModContent.ItemType<HolyMantle>(), 1).
                AddIngredient(ModContent.ItemType<VoidBar>(), 10).
                AddTile(ModContent.TileType<VoidWellTile>()).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient(ItemID.AnkhShield, 1).
                AddIngredient(ItemID.HeroShield, 1).
                AddIngredient(ModContent.ItemType<HolyMantle>(), 1).
                AddIngredient(ModContent.ItemType<ChaoticPiece>(), 15).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
