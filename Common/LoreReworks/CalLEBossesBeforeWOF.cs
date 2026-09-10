using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Core.Dash;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.LoreReworks
{
    public class LEKingSlime : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreKingSlime;
        public override void UpdateEffects(Player player)
        {
            player.jumpSpeedBoost += 1f;
        }
    }

    public class LEDesertScourge : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreDesertScourge;
        public override void UpdateEffects(Player player)
        {
            player.breathMax += 40;
        }
    }

    public class LEEOC : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreEyeofCthulhu;
        public static float Value = 0.04f;
        public override void UpdateEffects(Player player)
        {
            // 3.33 是 DashCD -= 0.04;该字段已删,改写自研冲刺锁定帧倍率
            player.GetModPlayer<CEDashPlayer>().CooldownMult -= Value;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", Value.ToPercent().ToString());
        }
    }

    public class LECabulon : LoreEffect
    {
        public static int BuffTime = 5;
        public override int ItemType => CEID.Item_LoreCrabulon;
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", BuffTime.ToString());
        }
    }

    public class LEBoc : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreBrainofCthulhu;
        public override void UpdateEffects(Player player)
        {
            player.buffImmune[BuffID.Bleeding] = true;
        }
    }

    public class LEEOW : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreEaterofWorlds;
        public override void UpdateEffects(Player player)
        {
            player.buffImmune[BuffID.CursedInferno] = true;
        }
    }

    public class LEHiveCrimson : LoreEffect
    {
        public override int ItemType => CEID.Item_LorePerforators;
        public static int HealAmount = 20;
        public static float chance = 0.15f;
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", chance.ToPercent().ToString());
            tooltip.Text = tooltip.Text.Replace("{2}", HealAmount.ToString());
        }
    }

    public class LEHiveCorrupt : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreHiveMind;
        public static float DamageAddition = 0.04f;
        public static int TimeSec = 3;
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", TimeSec.ToString());
            tooltip.Text = tooltip.Text.Replace("{2}", DamageAddition.ToPercent().ToString());
        }
    }

    public class LEQueenBee : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreQueenBee;
        public static float Damage = 0.05f;
        public static int CritDecrese = 4;
        public override void UpdateEffects(Player player)
        {
            player.GetDamage(DamageClass.Generic) += Damage;
            player.GetCritChance(DamageClass.Generic) -= CritDecrese;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", Damage.ToPercent().ToString());
            tooltip.Text = tooltip.Text.Replace("{2}", CritDecrese.ToString());
        }
    }

    public class LESkeletron : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreSkeletron;
        public static float Perc = 0.15f;
        public static int AmountLimit = 300;
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", AmountLimit.ToString());
            tooltip.Text = tooltip.Text.Replace("{2}", Perc.ToPercent().ToString());
        }
    }

    public class LESlimeGod : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreSlimeGod;
        public static float JumpSpeedBoost = 1;
        public override void UpdateEffects(Player player)
        {
            player.jumpSpeedBoost += JumpSpeedBoost;
        }
    }
}
