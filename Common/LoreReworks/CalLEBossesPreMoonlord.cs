using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.LoreReworks
{
    public class LEWof : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreWallofFlesh;
        public static int Cooldown = 20;
        public static float DmgReduce = 0.05f;
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DmgReduce.ToPercent().ToString());
            tooltip.Text = tooltip.Text.Replace("{2}", Cooldown.ToString());
        }
    }

    public class LEQueenSlime : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreQueenSlime;
        public static float FallingSpeedAdd = 0.05f;
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", FallingSpeedAdd.ToPercent().ToString());
        }
        public override void UpdateEffects(Player player)
        {
            player.Entropy().FallSpeed += FallingSpeedAdd;
        }
    }

    public class LECryo : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreArchmage;
        public override void UpdateEffects(Player player)
        {
            for (int b = 0; b < Player.MaxBuffs; b++)
            {
                if (player.buffType[b] == BuffID.Chilled || player.buffType[b] == BuffID.Frozen)
                {
                    if (player.buffTime[b] > 2 && Main.GameUpdateCount % 2 == 0)
                    {
                        player.buffTime[b] -= 1;
                    }
                }
            }
        }
    }

    public class LEMech : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreMechs;
        public static int DEF = 1;
        public override void UpdateEffects(Player player)
        {
            player.statDefense += DEF;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DEF.ToString());
        }
    }

    public class LETwin : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreTwins;
        public static int DEF = 1;
        public override void UpdateEffects(Player player)
        {
            player.statDefense += DEF;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DEF.ToString());
        }
    }

    public class LEDestroyer : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreDestroyer;
        public static int DEF = 1;
        public override void UpdateEffects(Player player)
        {
            player.statDefense += DEF;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DEF.ToString());
        }
    }

    public class LESkePrime : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreSkeletronPrime;
        public static float DR = 0.005f;
        public override void UpdateEffects(Player player)
        {
            player.endurance += DR;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DR.ToPercent().ToString());
        }
    }

    public class LESulphurSea : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreSulphurSea;
        public override void UpdateEffects(Player player)
        {
            player.buffImmune[BuffID.Venom] = true;
        }
    }

    public class LEAzafure : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreAzafure;
        public static int Crit = 1;
        public override void UpdateEffects(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += Crit;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", Crit.ToString());
        }
    }

    public class LECalClone : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreCalamitasClone;
        public static float Ats = 0.02f;
        public override void UpdateEffects(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += Ats;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", Ats.ToPercent().ToString());
        }
    }

    public class LEPlantera : LoreEffect
    {
        public override int ItemType => CEID.Item_LorePlantera;
        public static int Regen = 1;
        public override void UpdateEffects(Player player)
        {
            player.lifeRegen += Regen;
        }
    }

    public class LEAquaticScourge : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreAquaticScourge;
        public override void UpdateEffects(Player player)
        {
            player.breathMax += 60;
        }
    }

    public class LEAbyss : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreAbyss;
        public override void UpdateEffects(Player player)
        {
            player.Entropy().AbyssalLight += 0.18f;
        }
    }

    public class LELeviathan : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreLeviathanAnahita;
        public static int Regen = 2;
        public override void UpdateEffects(Player player)
        {
            if (player.wet)
            {
                player.lifeRegen += Regen;
            }
        }
    }

    public class LEAstrumAureus : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreAstrumAureus;
        public override void UpdateEffects(Player player)
        {
            int buff = CEID.Buff_AstralInjectionBuff;
            if (buff <= 0)
            {
                return;
            }
            for (int b = 0; b < Player.MaxBuffs; b++)
            {
                if (player.buffType[b] == buff)
                {
                    if (player.buffTime[b] > 2 && Main.GameUpdateCount % 2 == 0)
                    {
                        player.buffTime[b] -= 1;
                    }
                }
            }
        }
    }

    public class LEBrimElemental : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreBrimstoneElemental;
        public override void UpdateEffects(Player player)
        {
            player.endurance += 0.01f;
        }
    }

    public class LEGolem : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreGolem;
        public static float DR = 0.005f;
        public override void UpdateEffects(Player player)
        {
            player.endurance += DR;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DR.ToPercent().ToString());
        }
    }

    public class LEPlagueBee : LoreEffect
    {
        public override int ItemType => CEID.Item_LorePlaguebringerGoliath;
        public override void UpdateEffects(Player player)
        {
            int buff = CEID.Buff_Plague;
            if (buff <= 0)
            {
                return;
            }
            for (int b = 0; b < Player.MaxBuffs; b++)
            {
                if (player.buffType[b] == buff)
                {
                    if (player.buffTime[b] > 2 && Main.GameUpdateCount % 2 == 0)
                    {
                        player.buffTime[b] -= 2;
                    }
                }
            }
        }
    }

    public class LELightEmpress : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreEmpressofLight;
        public static int DEF = 2;
        public override void UpdateEffects(Player player)
        {
            if (Main.dayTime)
            {
                player.statDefense += DEF;
            }
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", DEF.ToString());
        }
    }

    public class LEFishron : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreDukeFishron;
        public override void UpdateEffects(Player player)
        {
            player.ignoreWater = true;
        }
    }

    public class LERavager : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreRavager;
        public override void UpdateEffects(Player player)
        {
            player.Entropy().moveSpeed += 0.02f;
        }
    }

    public class LEPrelude : LoreEffect
    {
        public override int ItemType => CEID.Item_LorePrelude;
        public override void UpdateEffects(Player player)
        {
            player.Entropy().light += 0.05f;
        }
    }

    public class LEAstral : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreAstralInfection;
        public static float WingTime = 0.05f;
        public override void UpdateEffects(Player player)
        {
            player.Entropy().WingTimeMult += WingTime;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", WingTime.ToPercent().ToString());
        }
    }

    public class LEDeus : LoreEffect
    {
        public override int ItemType => CEID.Item_LoreAstrumDeus;
        public static float WingSpeed = 0.03f;
        public override void UpdateEffects(Player player)
        {
            player.Entropy().WingSpeed += WingSpeed;
        }
        public override void ModifyTooltip(TooltipLine tooltip)
        {
            tooltip.Text = tooltip.Text.Replace("{1}", WingSpeed.ToPercent().ToString());
        }
    }
}
