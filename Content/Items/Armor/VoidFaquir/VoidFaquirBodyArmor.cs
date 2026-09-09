using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Body)]
    public class VoidFaquirBodyArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false;
        }
        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                EquipLoader.AddEquipTexture(Mod, "CalamityEntropy/Content/Items/Armor/VoidFaquir/VoidFaquirBodyArmor_Back", EquipType.Back, this);
            }
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 34;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.defense = 44;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.12f;
            player.GetCritChance(DamageClass.Generic) += 5;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_TwistingNether))
            {
                CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 18)
                .AddIngredient(CEID.Item_TwistingNether, 5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 18)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
