using CalamityEntropy.Content.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.AzafureMiners
{
    public class AzafureMiner : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 92;
            Item.height = 50;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 6;
            Item.useTime = 6;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<AzafureMinerTile>();
            Item.rare = ItemRarityID.Orange;
            //脱离灾厄:原灾厄RarityOrangeBuyPrice,按rarity-map实值表Orange=5金
            Item.value = Item.buyPrice(gold: 5);
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_EnergyCore, CEID.Item_DubiousPlating))
            {
                CreateRecipe().AddIngredient(CEID.Item_EnergyCore)
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(CEID.Item_DubiousPlating, 6)
                .AddRecipeGroup(CERecipeGroups.IronBar, 2)
                .AddTile(TileID.HeavyWorkBench)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>()
                .AddIngredient(ItemID.CobaltBar, 10)
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddIngredient(ItemID.MeteoriteBar, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>()
                .AddIngredient(ItemID.PalladiumBar, 10)
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddIngredient(ItemID.MeteoriteBar, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
