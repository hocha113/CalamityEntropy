using CalamityEntropy.Content.Items.Armor.Azafure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.AzafureT3
{
    [AutoloadEquip(EquipType.Legs)]
    public class AcropolisHeavyLegArmor : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 18;
            Item.value = Item.buyPrice(platinum: 1);
            Item.defense = 18;
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().moveSpeed += 0.15f;
            player.jumpSpeedBoost += 0.2f;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_UnholyEssence))
            {
                CreateRecipe()
                .AddIngredient<AzafureSteamKnightLeggings>()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddIngredient(CEID.Item_UnholyEssence, 4)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<AzafureSteamKnightLeggings>()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

}
