using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using CalamityMod.Systems.Collections;
using CalamityMod.Tiles.Furniture.CraftingStations;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ReincarnationBadge : ModItem
    {
        public override void SetStaticDefaults()
        {
            CalamityItemSets.HasAccessoryKeybind[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 98;
            Item.height = 60;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
            Item.accessory = true;

        }
        public override void ModifyTooltips(List<TooltipLine> list)
        {
            var k = Main.LocalPlayer.Calamity().FindAccessory<ReincarnationBadge>().GetDynamicModHotkey();
            list.IntegrateHotkey(k);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().reincarnationBadge = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ModContent.ItemType<AscendantInsignia>())
                .AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 4)
                .AddTile(ModContent.TileType<CosmicAnvil>()).Register();
        }
    }
}
