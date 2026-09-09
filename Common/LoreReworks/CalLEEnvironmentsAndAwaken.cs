using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.LoreReworks
{
    public class LECrimson : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreCrimson;
        public static int LifeAddition = 10;
        public override void UpdateEffects(Player player)
        {
            player.statLifeMax2 += LifeAddition;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", LifeAddition.ToString());
        }
    }

    public class LECorruption : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreCorruption;
        public static float DR = 0.01f;
        public override void UpdateEffects(Player player)
        {
            player.endurance += DR;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DR.ToPercent().ToString());
        }
    }

    public class LEUnderworld : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreUnderworld;
        public static int LavaImmuneTimeSec = 3;
        public override void UpdateEffects(Player player)
        {
            player.lavaMax += LavaImmuneTimeSec * 60;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", LavaImmuneTimeSec.ToString());
        }
    }

    public class LEBloodMoon : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreBloodMoon;
    }

    public class LEAwaken : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreAwakening;
        public override void UpdateEffects(Player player)
        {
            player.Entropy().moveSpeed += 0.005f;
            player.jumpSpeedBoost += 0.05f;
        }
    }
}
