using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.Smoldering
{
    [AutoloadEquip(EquipType.Legs)]
    public class SmolderingGreaves : ModItem
    {
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 24;
            Item.value = Item.buyPrice(gold: 10);
            Item.defense = 6;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateEquip(Player player) {
            player.Entropy().moveSpeed += 0.4f;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<TectonicShard>(6)
                .AddIngredient(ItemID.MoltenGreaves)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }
}
