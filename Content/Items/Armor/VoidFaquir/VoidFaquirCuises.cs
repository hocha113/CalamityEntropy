using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

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
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.defense = 36;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().VFLeg = true;
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.GetCritChance(DamageClass.Generic) += 10;
            player.Entropy().moveSpeed += 0.20f;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_TwistingNether))
            {
                CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 12)
                .AddIngredient(CEID.Item_TwistingNether, 3)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 10)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
