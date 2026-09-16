using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Lores
{
    public class ProphetLore : CELoreItem
    {
        public static float ImmuneAdd = 0.5f;
        public static int LifeRegen = 1;
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            base.ModifyTooltips(tooltips);
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                return;
            if (LoreEffect.Enabled) {
                TooltipLine tooltipLineEF = new TooltipLine(Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy.UseToggle"));
                tooltips.Add(tooltipLineEF);
                TooltipLine tooltipLineA = new TooltipLine(Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy.ProphetLoreEffect"));
                tooltipLineA.Text = tooltipLineA.Text.Replace("{2}", LifeRegen.ToString());
                tooltips.Add(tooltipLineA);

                TooltipLine tooltipLineE = new TooltipLine(Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy." + (Main.LocalPlayer.Entropy().ProphetLoreBonus ? "Enabled" : "Disabled")));
                tooltipLineE.OverrideColor = Main.LocalPlayer.Entropy().ProphetLoreBonus ? Color.Yellow : Color.Gray;
                tooltips.Add(tooltipLineE);
            }

        }
        public override bool CanUseItem(Player player) {
            return LoreEffect.Enabled;
        }
        public override void SetDefaults() {
            Item.width = 20;
            Item.height = 20;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Cyan;
            SoundStyle s = new("CalamityEntropy/Assets/Sounds/soulshine");
            Item.UseSound = s;
            Item.maxStack = 1;
            Item.useTurn = true;
        }
        public override bool? UseItem(Player player) {
            EModPlayer modPlayer = player.Entropy();
            player.itemTime = Item.useTime;
            modPlayer.ProphetLoreBonus = !modPlayer.ProphetLoreBonus;
            return true;
        }
    }
}
