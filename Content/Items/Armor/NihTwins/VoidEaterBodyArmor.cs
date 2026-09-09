using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.NihTwins
{
    [AutoloadEquip(EquipType.Body)]
    public class VoidEaterBodyArmor : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 42;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.defense = 35;
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.12f;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Necroplasm))
            {
                CreateRecipe()
                .AddIngredient<NihilityFragments>(8)
                .AddIngredient(CEID.Item_Necroplasm, 6)
                .AddIngredient(ItemID.LunarBar, 12)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            // 脱离灾厄:原 Necroplasm×6 换虚无碎片并与原有 8 枚合并
            CreateRecipe()
                .AddIngredient<NihilityFragments>(14)
                .AddIngredient(ItemID.LunarBar, 12)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
