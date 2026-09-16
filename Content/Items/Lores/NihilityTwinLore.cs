using CalamityEntropy.Common;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Lores
{
    public class NihilityTwinLore : CELoreItem
    {
        public static float VoidRes = 0.1f;
        public static int HealPreSec = 1;
        public static float MaxFlyTimeAddition = 0.05f;
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            base.ModifyTooltips(tooltips);
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                return;
            if (LoreEffect.Enabled) {
                TooltipLine tooltipLineEF = new TooltipLine(Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy.UseToggle"));
                tooltips.Add(tooltipLineEF);
                TooltipLine tooltipLineA = new TooltipLine(Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy.NihTwinLoreEffect"));
                tooltipLineA.Text = tooltipLineA.Text.Replace("{1}", VoidRes.ToPercent().ToString());
                tooltipLineA.Text = tooltipLineA.Text.Replace("{2}", HealPreSec.ToString());
                tooltipLineA.Text = tooltipLineA.Text.Replace("{3}", MaxFlyTimeAddition.ToPercent().ToString());

                tooltips.Add(tooltipLineA);

                TooltipLine tooltipLineE = new TooltipLine(Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy." + (Main.LocalPlayer.Entropy().NihilityTwinLoreBonus ? "Enabled" : "Disabled")));
                tooltipLineE.OverrideColor = Main.LocalPlayer.Entropy().NihilityTwinLoreBonus ? Color.Yellow : Color.Gray;
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
            Item.rare = ModContent.RarityType<NihilityBlue>();
            SoundStyle s = new("CalamityEntropy/Assets/Sounds/CastTriangles");
            s.Volume = 0.4f;
            s.Pitch = 1.4f;
            Item.UseSound = s;
            Item.maxStack = 1;
            Item.useTurn = true;
        }
        public override bool? UseItem(Player player) {
            EModPlayer modPlayer = player.Entropy();
            player.itemTime = Item.useTime;
            modPlayer.NihilityTwinLoreBonus = !modPlayer.NihilityTwinLoreBonus;
            return true;
        }
    }
}
