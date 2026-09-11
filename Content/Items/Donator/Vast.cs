using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.CalamityRef;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class Vast : ModItem, IDonatorItem
    {
        // 2026-08-31 平衡案:捐赠者更名
        public string DonatorName => "四九天宁";

        // 装灾厄走 3.33 成长(VastLV 标签),无灾厄保持 4.0 魔流层数机制
        // 饮用后2秒内缓慢额外恢复药水20%的魔力,魔法暴击给目标3秒灵魂紊乱,
        // 每消耗250魔力叠一层魔流(至多5层),每层+3%魔法暴击伤害。
        public const int ManaPerStack = 250;
        public const int MaxManaStacks = 5;
        public const float CritDamagePerStack = 0.03f;

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().addEquip("Vast", !hideVisual);
            if (!CERef.Has)
            {
                player.manaCost -= 0.10f;
                player.GetCritChance(DamageClass.Magic) += 5;
                player.GetDamage(DamageClass.Magic) += 0.05f;
                player.manaFlower = true;
                VastMPlayer vmp = player.GetModPlayer<VastMPlayer>();
                if (vmp.ExtraManaLv > 0)
                {
                    player.AddCritDamage(DamageClass.Magic, CritDamagePerStack * vmp.ExtraManaLv);
                }
                return;
            }
            //有灾厄不按 4.0 魔流 0-5 夹断;3.33 Level() 满档(幽花)是 7
            int level = Level();
            player.manaFlower = true;
            if (player.HasBuff(BuffID.ManaRegeneration))
            {
                player.GetCritChance(DamageClass.Magic) += 4;
            }
            for (int i = 0; i <= level; i++)
            {
                player.Entropy().addEquip("VastLV" + i);
            }
            player.manaCost -= 0.08f;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady())
            {
                CreateRecipe()
                    .AddIngredient(ItemID.Diamond)
                    .AddIngredient(ItemID.ManaFlower)
                    .AddIngredient(ItemID.ArcaneCrystal)
                    .NearShimmer()
                    .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.ArcaneFlower)
                .AddIngredient(ItemID.ArcaneCrystal)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 装灾厄叠回 3.33 的 TooltipBase + Level0..LevelN;无灾厄不碰,Items.Vast.Tooltip 保持 4.0
            if (!CERef.Has)
                return;

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

            int offset = 0;
            void InsertBlock(string keySuffix)
            {
                string key = $"Mods.{Mod.Name}.Items.{Name}.{keySuffix}";
                if (!Language.Exists(key))
                    return;
                string[] lines = Language.GetTextValue(key).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0)
                        continue;
                    tooltips.Insert(insertAt + offset, new TooltipLine(Mod, keySuffix + offset, line));
                    offset++;
                }
            }

            InsertBlock("TooltipBase");
            int level = Level();
            for (int i = 0; i <= level; i++)
            {
                InsertBlock("Level" + i);
            }
        }

        public static int Level()
        {
            if (!CERef.Has)
            {
                //占位:无灾厄支不读本方法
                return 0;
            }
            if (CECal.DownedPolterghast)
            {
                return 7;
            }
            if (NPC.downedMoonlord)
            {
                return 6;
            }
            if (EDownedBosses.downedProphet)
            {
                return 5;
            }
            if (CECal.DownedCryogen || CECal.DownedBrimstoneElemental)
            {
                return 4;
            }
            if (CECal.DownedSlimeGod)
            {
                return 3;
            }
            if (NPC.downedBoss2)
            {
                return 2;
            }
            if (NPC.downedSlimeKing || NPC.downedBoss1 || CECal.DownedDesertScourge)
            {
                return 1;
            }
            return 0;
        }
    }
    public class VastMPlayer : ModPlayer
    {
        public int ManaCostCount = 0;
        public int ExtraManaLv = 0;
        public int ExtraManaTime = 0;
        public bool BossClearFlag = false;
        public int LastMana = 0;
        /// <summary>魔力药水缓回:剩余帧与总池。</summary>
        public int PotRegenTime = 0;
        public int PotRegenPool = 0;

        public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
        {
            //4.0 魔流药水缓回;有灾厄不跑(3.33 无此钩)
            if (!CERef.Has && healValue > 0 && Player.Entropy().hasAcc("Vast"))
            {
                PotRegenPool = (int)(healValue * 0.2f);
                PotRegenTime = 120;
            }
        }

        public override void PostUpdate()
        {
            if (Player.dead)
                return;
            if (LastMana < Player.statMana)
            {
                LastMana = Player.statMana;
            }
            if (Player.statMana < LastMana)
            {
                ManaCostCount += LastMana - Player.statMana;
                LastMana = Player.statMana;
            }
            if (!CERef.Has)
            {
                if (!Player.Entropy().hasAcc("Vast"))
                {
                    ExtraManaLv = 0;
                    ExtraManaTime = 0;
                    PotRegenTime = 0;
                    return;
                }
                // 药水缓回:2秒内分10跳补足池子
                if (PotRegenTime > 0)
                {
                    PotRegenTime--;
                    if (PotRegenTime % 12 == 0 && PotRegenPool > 0)
                    {
                        int chunk = int.Max(1, PotRegenPool / 10);
                        Player.statMana = int.Min(Player.statManaMax2, Player.statMana + chunk);
                    }
                }
                // 每消耗250魔力叠一层魔流,至多5层
                if (ManaCostCount >= Vast.ManaPerStack)
                {
                    ManaCostCount -= Vast.ManaPerStack;
                    if (ExtraManaLv < Vast.MaxManaStacks)
                    {
                        ExtraManaLv++;
                    }
                    ExtraManaTime = 15 * 60;
                }
                if (ManaCostCount < 0)
                {
                    ManaCostCount = 0;
                }
                if (ExtraManaTime-- <= 0)
                {
                    ExtraManaLv = 0;
                }
                if (ExtraManaLv > 0)
                {
                    Player.AddBuff(ModContent.BuffType<ManaVein>(), 2);
                }
                SpawnManaFlowSmoke(Vast.MaxManaStacks);
                return;
            }

            //3.33:VastLV3 起叠 ExtraManaLv;不挂 4.0 ManaVein 图标
            if (!Player.Entropy().hasAcc("VastLV3"))
            {
                ExtraManaLv = 0;
                ExtraManaTime = 0;
                return;
            }
            if (!BossClearFlag && Main.CurrentFrameFlags.AnyActiveBossNPC && !CECal.IsBossRushActive)
            {
                ExtraManaTime = 0;
                Player.ClearBuff(ModContent.BuffType<ManaVein>());
            }
            BossClearFlag = Main.CurrentFrameFlags.AnyActiveBossNPC;
            if (ManaCostCount > 150 + Player.statManaMax / 10)
            {
                if (ExtraManaLv < 5)
                {
                    ExtraManaLv++;
                }
                ExtraManaTime = 15 * 60;
                if (ExtraManaLv == 5)
                {
                    ExtraManaTime = 15 * 60 * 5;
                }
                ManaCostCount -= 150 + Player.statManaMax / 10;
            }
            if (ManaCostCount < 0)
            {
                ManaCostCount = 0;
            }
            if (ExtraManaTime-- <= 0)
            {
                ExtraManaLv = 0;
            }
            if (Player.Entropy().hasAcc("VastLV3"))
            {
                Player.endurance += (Player.statManaMax2 - Player.Entropy().manaNorm) * 0.0003f;
            }
            if (Player.Entropy().hasAcc("VastLV5") && NPC.downedMoonlord)
            {
                Player.GetCritChance(DamageClass.Magic) += ExtraManaLv;
                Player.AddCritDamage(DamageClass.Magic, 0.03f * ExtraManaLv);
            }
            SpawnManaFlowSmoke(5);
        }

        private void SpawnManaFlowSmoke(int maxStacks)
        {
            for (int i = 0; i < ExtraManaLv; i++)
            {
                if (Main.rand.NextBool())
                {
                    //PRT_HeavySmokeCal CalamityPorts,Configure签名对齐Calamity原构造
                    PRTLoader.NewParticle<PRT_HeavySmokeCal>(Player.Center + new Vector2(Main.rand.NextFloat(-3, 3), Player.height / 2) + CEUtils.randomVec(1), CEUtils.randomVec(1), new Color(100, 100, 255), 0.16f).Configure(1, 40, 0.1f, true, 0, true);
                }
            }
            if (ExtraManaLv >= maxStacks)
            {
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(CEUtils.randomPoint(Player.getRect()), Player.velocity * 0.2f + CEUtils.randomVec(1), new Color(100, 100, 255), 0.2f).Configure(1, 40, 0.1f, true, 0, true);
            }
        }

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            //两侧 ExtraManaLv 语义不同,染色公式相同;0 层不染色
            r = float.Lerp(r, 0.5f, ExtraManaLv / 5f);
            g = float.Lerp(g, 0.5f, ExtraManaLv / 5f);
            b = float.Lerp(b, 1, ExtraManaLv / 5f);
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (Player.HasBuff<ManaAwaken>())
            {
                Player.HealMana(manaConsumed * 2);
            }
        }
    }
}
