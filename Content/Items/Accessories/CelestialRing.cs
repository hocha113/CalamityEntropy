using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class CelestialRing : ModItem
    {

        // 2026-08-31 平衡案重做:4防1自然生命再生,+2召唤栏,+15%召唤伤害,15%鞭子攻速与15%鞭子攻击距离
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.defense = 4;
            Item.height = 26;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.lifeRegen += 2;
            player.maxMinions += 2;
            player.GetDamage(DamageClass.Summon) += 0.15f;
            player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.15f;
            player.whipRangeMultiplier += 0.15f;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DarkSunRing, CEID.Item_AscendantSpiritEssence, CEID.Item_AuricBar, CEID.Tile_CosmicAnvil))
            {
                CreateRecipe().
                AddIngredient(ItemID.CelestialShell).
                AddIngredient(CEID.Item_DarkSunRing).
                AddIngredient(CEID.Item_AscendantSpiritEssence, 4).
                AddIngredient(CEID.Item_AuricBar, 5)
                .AddTile(CEID.Tile_CosmicAnvil).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient(ItemID.CelestialShell).
                AddIngredient(ItemID.PapyrusScarab).
                AddIngredient(ItemID.GoldRing).
                AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
