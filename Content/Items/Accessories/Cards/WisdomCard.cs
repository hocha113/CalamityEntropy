using CalamityEntropy.Common;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.Cards
{
    public class WisdomCard : ModItem
    {
        public static float ManaCostMul = 0.9f; //减少魔力消耗
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;

        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[COST]", (int)Math.Round((1 - ManaCostMul) * 100));
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<EModPlayer>().wisdomCard = true;
        }
    }
}
