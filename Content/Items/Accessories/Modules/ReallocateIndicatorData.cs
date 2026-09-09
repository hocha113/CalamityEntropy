using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Rarities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories.Modules
{
    public class RIDAtrPlayer : ModPlayer
    {
        public float Dmg;
        public float Asp;
        public float Def;
        public float DR;
        public float HP;
        public float Mana;
        public float SPD;
        public float WingTime;
        public int Crit;
    }
    public class ReallocateIndicatorData : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.accessory = true;
        }
        public static void CalculateStatsForPlayer(Player player)
        {
            float MaxDeviation = 0.4f;
            Item item = player.HeldItem;
            string name = item.type.ToString();
            if (item.ModItem != null)
            {
                name = item.ModItem.Name;
            }
            int seed = name.GetHashCode() + 14;
            seed += Main.worldName.GetHashCode() / 2 + player.name.GetHashCode() / 2;
            if (Main.zenithWorld)
            {
                seed += player.position.ToPoint().GetHashCode();
            }
            float sum = new UnifiedRandom(seed).NextFloat(0, MaxDeviation / 2f);
            if (player.AzafureEnhance())
            {
                sum += 0.38f;
            }
            List<float> ModifyMap = FloatListGenerator.GenerateFloatList(seed, 9, sum, -0.26f, 0.26f);
            float Dmg = ModifyMap[0];
            float Asp = ModifyMap[1] * 0.6f;
            float Def = ModifyMap[2] * 0.7f;
            float DR = ModifyMap[3] * 0.38f;
            float HP = ModifyMap[4];
            float Mana = ModifyMap[5];
            float SPD = ModifyMap[6];
            float WingTime = ModifyMap[7] * 2f;
            int Crit = (int)(ModifyMap[8] * 100);

            var mp = player.GetModPlayer<RIDAtrPlayer>();
            mp.Dmg = Dmg;
            mp.Asp = Asp;
            mp.Def = Def;
            mp.DR = DR;
            mp.HP = HP;
            mp.Mana = Mana;
            mp.SPD = SPD;
            mp.WingTime = WingTime;
            mp.Crit = Crit;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<HellIndustrialComponents>(2).AddCalOrOwn(CEID.Item_MysteriousCircuitry, ModContent.ItemType<AzafureCircuitry>(), 2).AddTile(TileID.WorkBenches).Register();
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.HeldItem.type > ItemID.None)
            {
                CalculateStatsForPlayer(player);
                ApplyStatModify(player);
            }
        }
        public static void ApplyStatModify(Player player)
        {
            var mp = player.GetModPlayer<RIDAtrPlayer>();
            DamageClass dc = DamageClass.Generic;
            player.GetDamage(dc) += mp.Dmg;
            player.GetAttackSpeed(dc) += mp.Asp;
            player.statDefense *= (1 + mp.Def);
            player.endurance += mp.DR;
            player.statLifeMax2 = (int)(player.statLifeMax2 * (1 + mp.HP));
            player.statManaMax2 = (int)(player.statManaMax2 * (1 + mp.Mana));
            player.Entropy().moveSpeed += mp.SPD;
            player.Entropy().WingSpeed += mp.SPD;
            player.Entropy().WingTimeMult += mp.WingTime;
            player.GetCritChance(dc) += mp.Crit;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            CalculateStatsForPlayer(Main.LocalPlayer);
            string tt = Mod.GetLocalization("RIDModifies").Value;
            if (Main.LocalPlayer.HeldItem.type > ItemID.None && Main.LocalPlayer.HeldItem.type != Item.type)
            {
                string NegCheck(float v)
                {
                    return v < 0 ? v.ToString() : "+" + v.ToString();
                }
                string NegCheckI(int v)
                {
                    return v < 0 ? v.ToString() : "+" + v.ToString();
                }
                var mp = Main.LocalPlayer.GetModPlayer<RIDAtrPlayer>();
                tt = tt.Replace("[1]", NegCheck(mp.Dmg.ToPercent()));
                tt = tt.Replace("[2]", NegCheck(mp.Asp.ToPercent()));
                tt = tt.Replace("[3]", NegCheck(mp.Def.ToPercent()));
                tt = tt.Replace("[4]", NegCheck(mp.DR.ToPercent()));
                tt = tt.Replace("[5]", NegCheck(mp.HP.ToPercent()));
                tt = tt.Replace("[6]", NegCheck(mp.Mana.ToPercent()));
                tt = tt.Replace("[7]", NegCheck(mp.SPD.ToPercent()));
                tt = tt.Replace("[8]", NegCheck(mp.WingTime.ToPercent()));
                tt = tt.Replace("[9]", NegCheckI(mp.Crit));
                tt = tt.Replace("[ITEMNAME]", Main.LocalPlayer.HeldItem.Name);
                tooltips.Add(new TooltipLine(Mod, "RID Attributes", tt) { OverrideColor = Color.Yellow });
            }
        }
        public class FloatListGenerator
        {
            public static List<float> GenerateFloatList(int seed, int length, float targetSum, float minValue, float maxValue)
            {
                float minPossibleSum = length * minValue;
                float maxPossibleSum = length * maxValue;

                if (targetSum < minPossibleSum)
                    targetSum = minPossibleSum;
                else if (targetSum > maxPossibleSum)
                    targetSum = maxPossibleSum;

                UnifiedRandom random = new UnifiedRandom(seed);

                List<float> result = new List<float>();

                for (int i = 0; i < length; i++)
                {
                    float randomValue = (float)(random.NextDouble() * (maxValue - minValue) + minValue);
                    result.Add(randomValue);
                }

                float currentSum = result.Sum();
                float difference = targetSum - currentSum;

                if (Math.Abs(difference) < 0.0001f)
                    return result;

                return AdjustListToTargetSum(seed, result, targetSum, minValue, maxValue, difference);
            }

            private static List<float> AdjustListToTargetSum(int seed, List<float> list, float targetSum,
                float minValue, float maxValue, float initialDifference)
            {
                float difference = initialDifference;
                UnifiedRandom random = new UnifiedRandom(seed);
                int maxAttempts = 1000; int attempts = 0;

                while (Math.Abs(difference) > 0.0001f && attempts < maxAttempts)
                {
                    bool adjusted = false;

                    for (int i = 0; i < list.Count && Math.Abs(difference) > 0.0001f; i++)
                    {
                        float currentValue = list[i];
                        float potentialAdjustment = difference / (list.Count - i);
                        float newValue = currentValue + potentialAdjustment;

                        if (newValue < minValue)
                            newValue = minValue;
                        else if (newValue > maxValue)
                            newValue = maxValue;

                        float actualAdjustment = newValue - currentValue;

                        if (Math.Abs(actualAdjustment) > 0.0001f)
                        {
                            list[i] = newValue;
                            difference -= actualAdjustment;
                            adjusted = true;
                        }
                    }

                    if (!adjusted && Math.Abs(difference) > 0.0001f)
                    {
                        int index1 = random.Next(list.Count);
                        int index2 = random.Next(list.Count);

                        if (index1 != index2)
                        {
                            float value1 = list[index1];
                            float value2 = list[index2];
                            float adjustment = Math.Min(Math.Abs(difference) / 2,
                                Math.Min(value1 - minValue, maxValue - value2));

                            if (adjustment > 0.0001f)
                            {
                                if (difference > 0)
                                {
                                    list[index1] = value1 - adjustment;
                                    list[index2] = value2 + adjustment;
                                    difference -= adjustment * 2;
                                }
                                else
                                {
                                    list[index1] = value1 + adjustment;
                                    list[index2] = value2 - adjustment;
                                    difference += adjustment * 2;
                                }
                            }
                        }
                    }

                    attempts++;
                }

                float finalSum = list.Sum();
                float finalDifference = targetSum - finalSum;

                if (Math.Abs(finalDifference) > 0.0001f)
                {
                    var adjustableItems = list.Select((value, index) => new { Value = value, Index = index })
                        .Where(x => (finalDifference > 0 && x.Value < maxValue) ||
                                   (finalDifference < 0 && x.Value > minValue))
                        .ToList();

                    if (adjustableItems.Any())
                    {
                        float adjustmentPerItem = finalDifference / adjustableItems.Count;

                        foreach (var item in adjustableItems)
                        {
                            float newValue = item.Value + adjustmentPerItem;
                            newValue = Math.Max(minValue, Math.Min(maxValue, newValue));
                            float actualAdjustment = newValue - item.Value;
                            list[item.Index] = newValue;
                            finalDifference -= actualAdjustment;
                        }
                    }
                }

                return list;
            }


        }
    }
}
