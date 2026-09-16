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
    public class CruiserLore : CELoreItem
    {
        public const int LifeBoost = 20;
        public const int ManaBoost = 50;
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            base.ModifyTooltips(tooltips);
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                return;
            if (LoreEffect.Enabled) {
                TooltipLine tooltipLineEF = new TooltipLine(base.Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy.UseToggle"));
                tooltips.Add(tooltipLineEF);
                TooltipLine tooltipLineA = new TooltipLine(base.Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy.loreCruiserEffect"));
                tooltips.Add(tooltipLineA);
                TooltipLine tooltipLineE = new TooltipLine(base.Mod, "Entropy:Effect", Language.GetTextValue("Mods.CalamityEntropy." + (Main.LocalPlayer.Entropy().CruiserLoreBonus ? "Enabled" : "Disabled")));
                tooltipLineE.OverrideColor = Main.LocalPlayer.Entropy().CruiserLoreBonus ? Color.Yellow : Color.Gray;
                tooltips.Add(tooltipLineE);
            }
        }
        public override void SetDefaults() {
            Item.width = 20;
            Item.height = 20;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ModContent.RarityType<VoidPurple>();
            SoundStyle s = new("CalamityEntropy/Assets/Sounds/vmspawn");
            s.Volume = 0.6f;
            s.Pitch = 1.4f;
            Item.UseSound = s;
            Item.maxStack = 1;
            Item.useTurn = true;
        }
        public override bool CanUseItem(Player player) {
            return LoreEffect.Enabled;
        }
        public override bool? UseItem(Player player) {
            EModPlayer modPlayer = player.Entropy();
            player.itemTime = Item.useTime;
            modPlayer.CruiserLoreBonus = !modPlayer.CruiserLoreBonus;
            return true;
        }
    }
}
