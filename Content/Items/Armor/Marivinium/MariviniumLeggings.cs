using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.Marivinium
{
    [AutoloadEquip(EquipType.Legs)]
    public class MariviniumLeggings : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 18;
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
            Item.defense = 50;
            Item.rare = ModContent.RarityType<AbyssalBlue>();
        }

        public override void UpdateEquip(Player player)
        {
            // 大幅机动性:30%移速与30%跳跃速度
            player.Entropy().moveSpeed += 0.30f;
            player.jumpSpeedBoost += 1.5f;
            player.GetDamage(DamageClass.Generic) += 0.05f;
            player.GetCritChance(DamageClass.Generic) += 5;

        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_OmegaBlueTentacles, ItemID.HallowedGreaves)
                .AddIngredient<WyrmTooth>(5)
                .AddIngredient<FadingRunestone>()
                .AddTile<AbyssalAltarTile>()
                .Register();
        }
    }

}
