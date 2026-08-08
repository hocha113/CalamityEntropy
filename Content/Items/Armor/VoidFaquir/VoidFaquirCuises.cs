using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Legs)]
    public class VoidFaquirCuises : ModItem, ILocalizedModType
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.defense = 39;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.GetCritChance(DamageClass.Generic) += 10;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 12)
                .AddIngredient(ModContent.ItemType<TwistingNether>(), 3)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
