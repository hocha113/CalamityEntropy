using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class WyrmToothNecklace : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 46;
            Item.height = 46;
            Item.accessory = true;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = ModContent.RarityType<AbyssalBlue>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage<GenericDamageClass>() += 0.3f;
            player.GetArmorPenetration<GenericDamageClass>() += 100;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddCalOrOwn(CEID.Item_ReaperToothNecklace, ItemID.SharkToothNecklace).
                AddIngredient<WyrmTooth>(9).
                AddIngredient<FadingRunestone>().
                AddTile(ModContent.TileType<AbyssalAltarTile>()).
                Register();
        }
    }
}
