using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Content.NPCs.Acropolis;
using CalamityEntropy.Content.NPCs.Apsychos;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.NPCs.LuminarisMoth;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using CalamityEntropy.Content.NPCs.Prophet;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Core.Integrations.BossLog;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Integrations
{
    /// <summary>
    /// 向 BossChecklist 登记本模组的 Boss 条目。未装 BossChecklist 时整段静默跳过。
    /// 原先是七段嵌套花括号块直写在模组入口的 PostSetupContent 里,现在收成一张表加一次循环。
    /// 带演员(<see cref="CEBossPortraitActor"/>)的条目同时登记到 <see cref="CEBossLogRegistry"/>,
    /// 由 <see cref="CEBossLogHook"/> 接管整本书;customPortrait 换成演员舞台,作钩子失败时的回退路径
    /// </summary>
    internal static class CEBossChecklistIntegration
    {
        /// <summary>
        /// 一个 Boss 条目。<paramref name="LocalizationName"/> 是取本地化键用的 NPC 内部名,
        /// 蠕虫类 Boss 必须挂头部的名字:BossChecklist 自身按
        /// Mods.&lt;模组&gt;.NPCs.&lt;头部NPC&gt;.BossChecklistIntegration.EntryName 取名,挂错位置会回退英文。
        /// <paramref name="EntryName"/> 是 BossChecklist 存档与标记用的键,历史拼写(如 AcropolisMechine)不可改。
        /// <paramref name="Actor"/> 为空时左页退回静态贴图 <paramref name="PortraitTexture"/>
        /// </summary>
        private readonly record struct BossEntry(
            string EntryName,
            string LocalizationName,
            float Difficulty,
            Func<bool> Downed,
            object NpcTypes,
            int SpawnItem,
            List<int> Collectibles,
            string PortraitTexture,
            float PortraitScale,
            bool MiniBoss = false,
            CEBossPortraitActor Actor = null);

        public static void Register() {
            if (!ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist) || bossChecklist == null) {
                return;
            }

            foreach (BossEntry entry in BuildEntries()) {
                Dictionary<string, object> extraInfo = new Dictionary<string, object>() {
                    ["displayName"] = Language.GetText($"Mods.CalamityEntropy.NPCs.{entry.LocalizationName}.BossChecklistIntegration.EntryName"),
                    ["spawnInfo"] = Language.GetText($"Mods.CalamityEntropy.NPCs.{entry.LocalizationName}.BossChecklistIntegration.SpawnInfo"),
                    ["despawnMessage"] = Language.GetText($"Mods.CalamityEntropy.NPCs.{entry.LocalizationName}.BossChecklistIntegration.DespawnMessage"),
                    ["collectibles"] = entry.Collectibles,
                    ["customPortrait"] = entry.Actor != null
                        ? MakeStagePortrait(entry.Actor)
                        : MakePortrait(entry.PortraitTexture, entry.PortraitScale)
                };
                if (entry.Actor != null) {
                    CEBossLogRegistry.Add(CalamityEntropy.Instance, entry.EntryName, entry.Actor, entry.Downed, entry.NpcTypes);
                }
                //没有召唤物的条目不能塞这个键,否则 BossChecklist 会当成"召唤物是 0 号物品"
                if (entry.SpawnItem > ItemID.None) {
                    extraInfo["spawnItems"] = entry.SpawnItem;
                }
                bossChecklist.Call(entry.MiniBoss ? "LogMiniBoss" : "LogBoss",
                    CalamityEntropy.Instance, entry.EntryName, entry.Difficulty, entry.Downed, entry.NpcTypes, extraInfo);
            }
        }

        private static Action<SpriteBatch, Rectangle, Color> MakePortrait(string texturePath, float scale) {
            return (SpriteBatch sb, Rectangle rect, Color color) => {
                Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;
                sb.Draw(texture, rect.Center.ToVector2(), null, color, 0, texture.Size() / 2, scale, SpriteEffects.None, 0);
            };
        }

        /// <summary>演员舞台版 customPortrait:整个左页矩形进来,舞台自己让出上游叠画的标题区</summary>
        private static Action<SpriteBatch, Rectangle, Color> MakeStagePortrait(CEBossPortraitActor actor) {
            return (SpriteBatch sb, Rectangle rect, Color color) => CEBossPortraitStage.Draw(sb, rect, color, actor);
        }

        //难度值即 BossChecklist 进度轴上的位置(它按这个值排序,不看登记顺序)。
        //本表的登记顺序沿用 3.33 原样,只是从七段花括号块改成了表项
        private static List<BossEntry> BuildEntries() {
            return new List<BossEntry>()
            {
                new BossEntry("AcropolisMechine", "AcropolisMachine", 2.8f,
                    () => EDownedBosses.downedAcropolis,
                    ModContent.NPCType<AcropolisMachine>(),
                    ItemID.None,
                    new List<int>(),
                    "CalamityEntropy/Assets/BCL/AcropolisMachine", 0.8f, MiniBoss: true,
                    Actor: AcropolisPortraitActor.Instance),

                new BossEntry("Apsychos", "Apsychos", 6.4f,
                    () => EDownedBosses.downedApsychos,
                    ModContent.NPCType<Apsychos>(),
                    ModContent.ItemType<CursedRunestone>(),
                    new List<int>(),
                    "CalamityEntropy/Assets/BCL/Apsychos", 0.36f,
                    Actor: ApsychosPortraitActor.Instance),

                new BossEntry("Luminaris", "Luminaris", 9.505f,
                    () => EDownedBosses.downedLuminaris,
                    ModContent.NPCType<Luminaris>(),
                    ModContent.ItemType<IllusionaryDew>(),
                    new List<int>(),
                    "CalamityEntropy/Assets/BCL/LuminarisBossCheckList", 1f,
                    Actor: LuminarisPortraitActor.Instance),

                new BossEntry("TheProphet", "TheProphet", 12.02f,
                    () => EDownedBosses.downedProphet,
                    ModContent.NPCType<TheProphet>(),
                    ModContent.ItemType<ProphecyToken>(),
                    new List<int>() {
                        ModContent.ItemType<RuneSong>(), ModContent.ItemType<UrnOfSouls>(), ModContent.ItemType<SpiritBanner>(),
                        ModContent.ItemType<ProphecyFlyingKnife>(), ModContent.ItemType<RuneMachineGun>(),
                        ModContent.ItemType<ForeseeOrb>(), ModContent.ItemType<RuneWing>()
                    },
                    "CalamityEntropy/Assets/BCL/Prophet", 1f,
                    Actor: ProphetPortraitActor.Instance),

                new BossEntry("NihilityTwin", "NihilityActeriophage", 19.3f,
                    () => EDownedBosses.downedNihilityTwin,
                    new List<int>() { ModContent.NPCType<NihilityActeriophage>(), ModContent.NPCType<ChaoticCell>() },
                    ModContent.ItemType<NihilityHorn>(),
                    new List<int>() {
                        ModContent.ItemType<NihilityTwinBag>(), ModContent.ItemType<NihilityTwinTrophy>(), ModContent.ItemType<NihilityTwinRelic>(),
                        ModContent.ItemType<NihilityShell>(), ModContent.ItemType<Voidseeker>(), ModContent.ItemType<EventideSniper>(),
                        ModContent.ItemType<NihilityBacteriophageWand>(), ModContent.ItemType<StarlessNight>(), ModContent.ItemType<VoidPathology>()
                    },
                    "CalamityEntropy/Assets/BCL/NihilityTwin", 0.7f,
                    Actor: NihilityPortraitActor.Instance),

                new BossEntry("Cruiser", "CruiserHead", 22.1f,
                    () => EDownedBosses.downedCruiser,
                    new List<int>() { ModContent.NPCType<CruiserHead>(), ModContent.NPCType<CruiserBody>(), ModContent.NPCType<CruiserTail>() },
                    ModContent.ItemType<VoidBottle>(),
                    new List<int>() {
                        ModContent.ItemType<CruiserBag>(), ModContent.ItemType<CruiserTrophy>(), ModContent.ItemType<VoidScales>(),
                        ModContent.ItemType<VoidMonolith>(), ModContent.ItemType<CruiserRelic>(), ModContent.ItemType<VoidRelics>(),
                        ModContent.ItemType<VoidAnnihilate>(), ModContent.ItemType<VoidElytra>(), ModContent.ItemType<VoidEcho>(),
                        ModContent.ItemType<Content.Items.Weapons.Silence>(), ModContent.ItemType<WingsOfHush>(), ModContent.ItemType<WindOfUndertaker>(),
                        ModContent.ItemType<VoidToy>(), ModContent.ItemType<TheocracyPearlToy>(), ModContent.ItemType<CruiserPlush>()
                    },
                    "CalamityEntropy/Assets/BCL/Cruiser", 0.7f,
                    Actor: CruiserPortraitActor.Instance),

                //虚空驱逐舰:月后 T2 空位,介于虚无双子 19.3 与巡游者 22.1 之间
                new BossEntry("VoidDestroyer", "VoidDestroyer", 20.8f,
                    () => EDownedBosses.downedVoidDestroyer,
                    ModContent.NPCType<VoidDestroyer>(),
                    ModContent.ItemType<VoidTransmitter>(),
                    new List<int>() {
                        ModContent.ItemType<VoidDestroyerBag>(), ModContent.ItemType<VoidDestroyerTrophy>(),
                        ModContent.ItemType<VoidDestroyerRelic>(), ModContent.ItemType<DimBearing>()
                    },
                    "CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerP2", 1f,
                    Actor: VoidDestroyerPortraitActor.Instance)
            };
        }
    }
}
