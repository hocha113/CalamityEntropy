using CalamityEntropy.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.EvilCards
{
    public class Barren : ModItem
    {

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<EModPlayer>().BarrenCard = true;
            // 脱离灾厄:原盗贼减益按「盗贼→全伤害」规则转通用
            player.GetDamage(DamageClass.Generic) -= 0.05f;
            player.GetCritChance(DamageClass.Generic) -= 5;
        }

        public override void AddRecipes() {
        }
    }
}
