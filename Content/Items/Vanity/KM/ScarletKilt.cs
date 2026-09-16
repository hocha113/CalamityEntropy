using CalamityEntropy.Content.Items.Donator;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity.KM
{
    [AutoloadEquip(EquipType.Legs)]
    public class ScarletKilt : ModItem, IDonatorItem
    {
        public string DonatorName => "黯月殇梦";
        public override void SetDefaults() {
            Item.width = 48;
            Item.height = 48;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.vanity = true;
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.Silk, 6)
                .AddIngredient(ItemID.Ruby, 2)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
}
