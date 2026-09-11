using System;
using System.Collections.Generic;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class Leyla : ModItem, IDonatorItem
    {
        public string DonatorName => "Fortun3Rod1on";
        // 装灾厄走 3.33 成长阶梯,无灾厄保持 4.0 常数(+50% DoT)
        public static float DoTBonus => CERef.Has ? DoTDmgMult(Level()) : 0.5f;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
            Item.defense = CERef.Has ? 0 : 6;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().addEquip("Leyla", !hideVisual);
            player.Entropy().leylaAura = true;
            if (!CERef.Has)
            {
                player.statLifeMax2 += 40;
                player.lifeRegen += 4;
                return;
            }
            int level = Level();
            player.statDefense += GetDefense(level);
            player.endurance += GetEndurance(level);
            player.statLifeMax2 += MaxHealthAddition(level);
            player.lifeRegen += (int)(Math.Round(GetRegen(level) * 2));
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady())
            {
                CreateRecipe()
                    .AddIngredient(ItemID.Sunflower)
                    .AddIngredient(ItemID.FallenStar, 5)
                    .AddIngredient(ItemID.Ruby, 2)
                    .AddTile(TileID.WorkBenches)
                    .NearShimmer()
                    .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Accessories.SilvasCrown>())
                .AddIngredient(ItemID.BottledHoney, 10)
                .AddIngredient(ItemID.Sunflower)
                .AddIngredient(ItemID.Ruby, 2)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 装灾厄整段换成灾厄时代键,无灾厄不碰,Items.Leyla.Tooltip 保持 4.0 原文。
            // 4.0 那段是静态文案,没有占位符,所以替换只在灾厄支跑。
            if (!CERef.Has)
                return;
            string cal = Mod.GetLocalization("LeylaCal").Value;
            int insertAt = -1;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                {
                    insertAt = i;
                    break;
                }
            }
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                    tooltips.RemoveAt(i);
            }
            if (insertAt < 0)
                insertAt = tooltips.Count;
            string[] lines = cal.Replace("\r\n", "\n").Split('\n');
            int offset = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                tooltips.Insert(insertAt + offset, new TooltipLine(Mod, "TooltipCal" + offset, line));
                offset++;
            }
            int level = Level();
            tooltips.Replace("[1]", GetDefense(level));
            tooltips.Replace("[2]", GetRegen(level).ToString());
            tooltips.Replace("[3]", GetEndurance(level).ToPercent());
            tooltips.Replace("[4]", DoTDmgMult(level).ToPercent());
            tooltips.Replace("[5]", MaxHealthAddition(level));
            tooltips.Replace("[L]", level);
            tooltips.Replace("[ML]", 9);
        }

        /// <summary>无灾厄固定三条原版减益;装灾厄按进度解锁 PortsDoT。</summary>
        public static List<int> ApplyBuffType()
        {
            if (!CERef.Has)
            {
                return new List<int>
                {
                    BuffID.Frostburn,
                    BuffID.Venom,
                    BuffID.CursedInferno
                };
            }
            List<int> l = new List<int>();
            if (CECal.DownedCalamitas(EDownedBosses.downedCruiser))
            {
                l.Add(ModContent.BuffType<TrueVulnerabilityHex>());
            }
            if (CECal.DownedExoMechs(EDownedBosses.downedCruiser))
            {
                l.Add(ModContent.BuffType<MiracleBlight>());
            }
            if (CECal.DownedYharon(EDownedBosses.downedCruiser))
            {
                l.Add(ModContent.BuffType<Dragonfire>());
            }
            if (CECal.DownedDoG(EDownedBosses.downedCruiser))
            {
                l.Add(ModContent.BuffType<GodSlayerInferno>());
            }
            if (CECal.DownedProvidence(EDownedBosses.downedNihilityTwin))
            {
                l.Add(ModContent.BuffType<HolyFlames>());
            }
            if (CECal.DownedBoomerDuke)
            {
                l.Add(ModContent.BuffType<SulphuricPoisoning>());
            }
            if (CECal.DownedPlaguebringer)
            {
                l.Add(ModContent.BuffType<Plague>());
            }
            if (CECal.DownedCryogen)
            {
                l.Add(BuffID.Frostburn2);
            }
            if (NPC.downedBoss2)
            {
                l.Add(BuffID.Venom);
            }
            l.Add(BuffID.Poisoned);
            return l;
        }
        public static int MaxHealthAddition(int level) => level switch
        {
            0 => 10,
            1 => 15,
            2 => 20,
            3 => 25,
            4 => 30,
            5 => 35,
            6 => 40,
            7 => 50,
            8 => 60,
            9 => 70,
            _ => 10
        };
        public static float DoTDmgMult(int level) => level switch
        {
            0 => 0.5f,
            1 => 0.75f,
            2 => 1f,
            3 => 1.2f,
            4 => 1.4f,
            5 => 1.5f,
            6 => 1.6f,
            7 => 1.8f,
            8 => 2f,
            9 => 2.5f,
            _ => 0.5f
        };
        public static float GetRegen(int level) => level switch
        {
            0 => 0.5f,
            1 => 1f,
            2 => 1.5f,
            3 => 2f,
            4 => 2.5f,
            5 => 3f,
            6 => 4f,
            7 => 5f,
            8 => 5.5f,
            9 => 6f,
            _ => 0.5f
        };
        public static float GetEndurance(int level) => level switch
        {
            0 => 0.02f,
            1 => 0.03f,
            2 => 0.04f,
            3 => 0.05f,
            4 => 0.06f,
            5 => 0.08f,
            6 => 0.09f,
            7 => 0.1f,
            8 => 0.12f,
            9 => 0.14f,
            _ => 0.02f
        };
        public static int GetDefense(int level) => level switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 5,
            4 => 6,
            5 => 8,
            6 => 10,
            7 => 12,
            8 => 14,
            9 => 16,
            _ => 1
        };

        private static uint levelCacheFrame = uint.MaxValue;
        private static int levelCache;

        public static int Level()
        {
            if (levelCacheFrame == Main.GameUpdateCount)
            {
                return levelCache;
            }
            levelCacheFrame = Main.GameUpdateCount;
            if (!CERef.Has)
            {
                //占位:无灾厄支不读本方法
                levelCache = 6;
                return levelCache;
            }
            if (CECal.DownedCalamitas(EDownedBosses.downedCruiser) && CECal.DownedExoMechs(EDownedBosses.downedCruiser))
            {
                levelCache = 9;
                return levelCache;
            }
            if (CECal.DownedDoG(EDownedBosses.downedCruiser))
            {
                levelCache = 8;
                return levelCache;
            }
            if (CECal.DownedProvidence(EDownedBosses.downedNihilityTwin))
            {
                levelCache = 7;
                return levelCache;
            }
            if (NPC.downedMoonlord)
            {
                levelCache = 6;
                return levelCache;
            }
            if (NPC.downedPlantBoss)
            {
                levelCache = 5;
                return levelCache;
            }
            if (CECal.DownedCryogen || CECal.DownedBrimstoneElemental)
            {
                levelCache = 4;
                return levelCache;
            }
            if (CECal.DownedSlimeGod)
            {
                levelCache = 3;
                return levelCache;
            }
            if (NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind)
            {
                levelCache = 2;
                return levelCache;
            }
            if (NPC.downedSlimeKing || NPC.downedBoss1 || CECal.DownedDesertScourge)
            {
                levelCache = 1;
                return levelCache;
            }
            levelCache = 0;
            return levelCache;
        }
    }
}
