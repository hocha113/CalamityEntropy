using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.NihTwins
{
    [AutoloadEquip(EquipType.Legs)]
    public class VoidEaterLeggings : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.defense = 20;
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }

        public override void UpdateEquip(Player player)
        {
            player.Entropy().moveSpeed += 0.18f;
            player.GetDamage(DamageClass.Generic) += 0.08f;
            player.GetCritChance(DamageClass.Generic) += 8;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Necroplasm))
            {
                CreateRecipe()
                .AddIngredient<NihilityFragments>(5)
                .AddIngredient(CEID.Item_Necroplasm, 6)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            // 脱离灾厄:原 Necroplasm×6 换虚无碎片并与原有 5 枚合并
            CreateRecipe()
                .AddIngredient<NihilityFragments>(11)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

}
