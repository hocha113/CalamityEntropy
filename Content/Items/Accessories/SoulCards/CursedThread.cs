using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories.SoulCards
{
    public class CursedThread : ModItem
    {
        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.material = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
                tooltips.FuckThisTooltipAndReplace($"{CEUtils.LocalPrefix}.Items.{GetType().Name}.HoldShiftForDetails");
        }
    }
}
