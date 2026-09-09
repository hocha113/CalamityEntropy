using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items;

public class SignalBlocker : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(0, 0, 0, 20);
    }

    public override void UpdateInventory(Player player)
    {
        if (Item.favorited)
        {
            EModSys.AcropolisDontSpawn = 5;
        }
    }

    public override void AddRecipes()
    {
        CreateRecipe().AddIngredient<HellIndustrialComponents>(3).
            AddCalOrOwn(CEID.Item_MysteriousCircuitry, ModContent.ItemType<AzafureCircuitry>()).
            AddTile(TileID.WorkBenches).
            Register();
    }
}
