using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Accessories
{
        // 装灾厄走 3.33 的 11 档通用加成,无灾厄保持 4.0 近战成长;Tooltip 保持 4.0
    // 全程免疫击退,近战伤害/暴击随击败Boss成长(终阶18%伤害/5%暴击)。
    public class SusiesBracelet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.noKnockback = true;
            if (!CERef.Has)
            {
                player.GetDamage(DamageClass.Melee) += AddMeleeDamage;
                player.GetCritChance(DamageClass.Melee) += AddMeleeCrit;
                return;
            }
            player.statDefense += AddDef;
            player.GetDamage(DamageClass.Generic) += AddDamage;
            player.statLifeMax2 += AddHP;
            player.statManaMax2 += AddMana;
        }

        public int Level = 0;
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Level);
        }
        public override void NetReceive(BinaryReader reader)
        {
            Level = reader.ReadInt32();
        }
        public override void SaveData(TagCompound tag)
        {
            if (Level > 0)
                tag["Level"] = Level;
        }
        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet<int>("Level", out int lv))
            {
                Level = lv;
            }
        }
        public int GetLevel()
        {
            CheckUpdate();
            return Level;
        }

        public void CheckUpdate()
        {
            void Check(bool f, int lv)
            {
                if (lv > Level && f)
                {
                    Level = lv;
                }
            }
            if (!CERef.Has)
            {
                Check(NPC.downedSlimeKing, 1);
                Check(NPC.downedBoss1, 2);
                Check(NPC.downedBoss2, 3);
                Check(NPC.downedBoss3, 4);
                Check(Main.hardMode, 5);
                Check(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3, 6);
                Check(NPC.downedPlantBoss, 7);
                Check(NPC.downedGolemBoss, 8);
                Check(EDownedBosses.downedProphet, 9);
                Check(NPC.downedMoonlord, 10);
                return;
            }
            Check(NPC.downedBoss1 || NPC.downedSlimeKing, 1);
            Check(NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind, 2);
            Check(NPC.downedBoss3 || NPC.downedQueenBee, 3);
            Check(Main.hardMode, 4);
            Check(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3, 5);
            Check(NPC.downedPlantBoss || CECal.DownedCalamitasClone(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3), 6);
            Check(NPC.downedMoonlord, 7);
            Check(CECal.DownedProvidence(EDownedBosses.downedNihilityTwin), 8);
            Check(CECal.DownedPolterghast || CECal.DownedStormWeaver || CECal.DownedCeaselessVoid || CECal.DownedSignus, 9);
            Check(CECal.DownedDoG(EDownedBosses.downedCruiser), 10);
            Check(CECal.DownedYharon(EDownedBosses.downedCruiser), 11);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int index = 0;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name.Contains("Tooltip"))
                {
                    index = i;
                }
            }
            index++;
            if (GetLevel() < 10)
            {
                tooltips.Add(new TooltipLine(Mod, $"Tooltip{index}", GetLt($"Trial", "Trials").Value + $"{GetLevel() + 1} - " + GetLt($"t{GetLevel()}", "Trials").Value) { OverrideColor = Color.Yellow });
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, $"Tooltip{index}", GetLt("t10", "Trials").Value) { OverrideColor = Color.Yellow });
            }

            tooltips.Add(new TooltipLine(Mod, $"Tooltip{index}", GetLt($"l{GetLevel()}").Value) { OverrideColor = Color.Pink });

            tooltips.Replace("[DMG]", AddMeleeDamage.ToPercent().ToString());
            tooltips.Replace("[CRIT]", AddMeleeCrit.ToString());
        }
        public float AddDamage => GetLevel() switch
        {
            0 => 0.02f,
            1 => 0.04f,
            2 => 0.06f,
            3 => 0.08f,
            4 => 0.1f,
            5 => 0.12f,
            6 => 0.14f,
            7 => 0.16f,
            8 => 0.18f,
            9 => 0.2f,
            10 => 0.22f,
            11 => 0.24f,
            _ => 0.24f
        };
        public int AddHP => GetLevel() switch
        {
            0 => 10,
            1 => 15,
            2 => 20,
            3 => 25,
            4 => 30,
            5 => 40,
            6 => 50,
            7 => 60,
            8 => 70,
            9 => 80,
            10 => 90,
            11 => 100,
            _ => 100
        };
        public int AddMana => GetLevel() switch
        {
            0 => 30,
            1 => 40,
            2 => 60,
            3 => 80,
            4 => 90,
            5 => 100,
            6 => 110,
            7 => 120,
            8 => 130,
            9 => 140,
            10 => 150,
            11 => 160,
            _ => 160
        };
        public int AddDef => GetLevel() switch
        {
            0 => 2,
            1 => 4,
            2 => 6,
            3 => 8,
            4 => 10,
            5 => 12,
            6 => 14,
            7 => 16,
            8 => 18,
            9 => 20,
            10 => 22,
            11 => 25,
            _ => 25
        };
        public float AddMeleeDamage => GetLevel() switch
        {
            0 => 0.01f,
            1 => 0.02f,
            2 => 0.03f,
            3 => 0.04f,
            4 => 0.05f,
            5 => 0.06f,
            6 => 0.07f,
            7 => 0.08f,
            8 => 0.10f,
            9 => 0.12f,
            _ => 0.18f
        };
        public int AddMeleeCrit => GetLevel() switch
        {
            0 => 0,
            1 => 0,
            2 => 0,
            3 => 1,
            4 => 1,
            5 => 2,
            6 => 2,
            7 => 3,
            8 => 4,
            9 => 4,
            _ => 5
        };
        public static LocalizedText GetLt(string n, string h = "Lores")
        {
            return Language.GetText($"Mods.CalamityEntropy.LegendaryAbility.SusiesBracelet.{h}.{n}");
        }
    }

    /// <summary>苏西腕带掉落:海龟 25%。</summary>
    public class SusiesBraceletDropGNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.SeaTurtle)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SusiesBracelet>(), 4));
            }
        }
    }
}
