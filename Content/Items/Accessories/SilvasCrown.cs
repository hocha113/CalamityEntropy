using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class SilvasCrown : ModItem
    {
        // 2026-08-31 平衡案重做:2防,+25最大生命,给予蜂蜜增益(原每秒回血效果退役)
        public override void SetDefaults() {
            Item.width = 42;
            Item.height = 42;
            Item.defense = 2;
            Item.value = Item.buyPrice(gold: 45);
            Item.rare = ModContent.RarityType<GlowGreen>();
            Item.accessory = true;


        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.statLifeMax2 += 25;
            player.AddBuff(BuffID.Honey, 2);
        }

    }
}
