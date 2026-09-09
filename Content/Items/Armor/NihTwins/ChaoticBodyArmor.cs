using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.NihTwins
{
    [AutoloadEquip(EquipType.Body)]
    public class ChaoticBodyArmor : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 42;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.defense = 40;
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += 8;
            player.endurance += 0.1f;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster))
            {
                CreateRecipe()
                .AddIngredient<ChaoticPiece>(8)
                .AddIngredient(CEID.Item_ExodiumCluster, 12)
                .AddIngredient(ItemID.LunarBar, 12)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<ChaoticPiece>(8)
                .AddIngredient(ItemID.LunarBar, 12)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
