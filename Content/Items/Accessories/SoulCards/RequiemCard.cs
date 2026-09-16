using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.SoulCards
{
    public class RequiemCard : ModItem
    {
        public static float CooldownDec = 0.10f;
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().CooldownTimeMult -= CooldownDec;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[T]", CooldownDec.ToPercent());
        }
    }
}
