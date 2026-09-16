using CalamityEntropy.Content.Items.Accessories.Cards;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.EvilCards
{
    public class ThreadOfAbyss : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<ThreadOfFate>();
        }
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.material = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                tooltips.FuckThisTooltipAndReplace($"{CEUtils.LocalPrefix}.Items.{GetType().Name}.HoldShiftForDetails");
        }
    }
}
