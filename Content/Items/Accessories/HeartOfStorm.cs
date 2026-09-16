using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class HeartOfStorm : ModItem
    {
        // 脱离灾厄:灾厄大修(CWR)联动提示与本地化注册随「灾厄家族软集成全删」裁决移除

        public override void SetDefaults() {
            Item.width = 52;
            Item.height = 52;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ModContent.RarityType<GlowPurple>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().heartOfStorm = true;
        }

        public override void AddRecipes() {
            // 2026-08-31 平衡案:暂由3星旋碎片+3夜明锭合成
            CreateRecipe()
                .AddIngredient(Terraria.ID.ItemID.FragmentVortex, 3)
                .AddIngredient(Terraria.ID.ItemID.LunarBar, 3)
                .AddTile(Terraria.ID.TileID.LunarCraftingStation)
                .Register();
        }
    }
}
