using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Armor;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ReincarnationBadge : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 98;
            Item.height = 60;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.accessory = true;

        }
        public override void ModifyTooltips(List<TooltipLine> list)
        {
            // 脱离灾厄:灾厄动态饰品键位并入自有 AccessoryAbilityHotKey(player-api.md §2)
            list.Replace("[KEY]", EModPlayer.AccessoryAbilityHotKey.TooltipKeyHint());
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().reincarnationBadge = true;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AscendantInsignia, CEID.Item_AscendantSpiritEssence, CEID.Tile_CosmicAnvil))
            {
                CreateRecipe().AddIngredient(CEID.Item_AscendantInsignia)
                .AddIngredient(CEID.Item_AscendantSpiritEssence, 4)
                .AddTile(CEID.Tile_CosmicAnvil).Register();
                return;
            }
            // 脱离灾厄:灾厄升华勋章改为原版飞升徽记(其灾厄配方本源),站台改远古操纵机
            CreateRecipe().AddIngredient(ItemID.EmpressFlightBooster)
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>().Register();
        }
    }
}
