using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.SandseaDragon;
using CalamityEntropy.Content.Items.SandseaDragon.Loot;
using CalamityEntropy.Content.Items.SunMoonGods;
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
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
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
    /// <summary>有演员的条目同时进 CEBossLogRegistry,customPortrait 是钩子失败时的回退</summary>
    internal static class CEBossChecklistIntegration
    {
        /// <summary>
        /// LocalizationName 要挂头部 NPC,BossChecklist 按 Mods.模组.NPCs.头部.BossChecklistIntegration.EntryName 取名,挂错回退英文
        /// EntryName 是存档键,AcropolisMechine 这种历史拼写不能改;Actor 为空时左页用 PortraitTexture
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
                //蜂王前,原版 3 与 4 之间
                new BossEntry("AcropolisMechine", "AcropolisMachine", 3.5f,
                    () => EDownedBosses.downedAcropolis,
                    ModContent.NPCType<AcropolisMachine>(),
                    ModContent.ItemType<SignalBlocker>(),
                    new List<int>(),
                    "CalamityEntropy/Assets/BCL/AcropolisMachine", 0.8f,
                    Actor: AcropolisPortraitActor.Instance),

                new BossEntry("Apsychos", "Apsychos", 6.4f,
                    () => EDownedBosses.downedApsychos,
                    ModContent.NPCType<Apsychos>(),
                    ModContent.ItemType<CursedRunestone>(),
                    new List<int>(),
                    "CalamityEntropy/Assets/BCL/Apsychos", 0.36f,
                    Actor: ApsychosPortraitActor.Instance),

                //机械骷髅王 11 与日食 11.5 之间
                new BossEntry("Luminaris", "Luminaris", 11.25f,
                    () => EDownedBosses.downedLuminaris,
                    ModContent.NPCType<Luminaris>(),
                    ModContent.ItemType<IllusionaryDew>(),
                    new List<int>(),
                    "CalamityEntropy/Assets/BCL/LuminarisBossCheckList", 1f,
                    Actor: LuminarisPortraitActor.Instance),

                //沙海冥龙:石巨人 13 之后,光之女皇 15 之前;没有演员,左页用拼好的立绘
                new BossEntry("SandseaDragon", "SandseaDragonHead", 13.5f,
                    () => EDownedBosses.downedSandseaDragon,
                    new List<int>() {
                        ModContent.NPCType<Content.NPCs.SandseaDragon.SandseaDragonHead>(),
                        ModContent.NPCType<Content.NPCs.SandseaDragon.SandseaDragonBody>(),
                        ModContent.NPCType<Content.NPCs.SandseaDragon.SandseaDragonTail>()
                    },
                    ModContent.ItemType<SandstormPendant>(),
                    new List<int>() {
                        ModContent.ItemType<SandseaDragonBag>(), ModContent.ItemType<SandseaDragonTrophy>(), ModContent.ItemType<SandseaDragonRelic>(),
                        ModContent.ItemType<SandseaDragonMask>(), ModContent.ItemType<SandseaDragonEgg>()
                    },
                    "CalamityEntropy/Assets/BCL/SandseaDragon", 0.45f),

                //先知:光之女皇 15 与双足翼龙 16 之后,教徒 17 之前
                new BossEntry("TheProphet", "TheProphet", 16.5f,
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

                //虚空驱逐舰:月亮领主 18 之后,虚无双子 19.3 之前
                new BossEntry("VoidDestroyer", "VoidDestroyer", 18.65f,
                    () => EDownedBosses.downedVoidDestroyer,
                    ModContent.NPCType<VoidDestroyer>(),
                    ModContent.ItemType<VoidTransmitter>(),
                    new List<int>() {
                        ModContent.ItemType<VoidDestroyerBag>(), ModContent.ItemType<VoidDestroyerTrophy>(),
                        ModContent.ItemType<VoidDestroyerRelic>(), ModContent.ItemType<DimBearing>()
                    },
                    "CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerP2", 1f,
                    Actor: VoidDestroyerPortraitActor.Instance),

                //日月双神:召唤物要驱逐舰掉的黯淡轴承,排在驱逐舰 18.65 之后、虚无双子 19.3 之前;没有演员,左页用拼好的立绘
                new BossEntry("SunGoddess", "SunGoddess", 18.8f,
                    () => EDownedBosses.downedSunGoddess,
                    ModContent.NPCType<SunGoddess>(),
                    ModContent.ItemType<SolunarRune>(),
                    new List<int>() { ModContent.ItemType<BlazingSunlight>(), ModContent.ItemType<PowerOfSunlight>() },
                    "CalamityEntropy/Assets/BCL/SunGoddess", 1f),

                new BossEntry("StarmoonSage", "StarmoonSage", 18.85f,
                    () => EDownedBosses.downedStarmoonSage,
                    ModContent.NPCType<StarmoonSage>(),
                    ModContent.ItemType<SolunarRune>(),
                    new List<int>() { ModContent.ItemType<WraithSoulEssence>(), ModContent.ItemType<PowerOfStarmoon>() },
                    "CalamityEntropy/Assets/BCL/StarmoonSage", 0.8f),

                //虚空史莱姆:排在虚无双子 19.3 之后、巡游者 22.1 之前
                new BossEntry("VoidSlime", "VoidSlime", 19.45f,
                    () => EDownedBosses.downedVoidSlime,
                    ModContent.NPCType<Content.NPCs.VoidSlime.VoidSlime>(),
                    ModContent.ItemType<Content.Items.VoidSlime.StarCrown>(),
                    new List<int>() {
                        ModContent.ItemType<Content.Items.VoidSlime.VoidSlimeBag>(), ModContent.ItemType<Content.Items.VoidSlime.VoidSlimeTrophy>(),
                        ModContent.ItemType<Content.Items.VoidSlime.VoidSlimeRelic>(), ModContent.ItemType<Content.Items.VoidSlime.BabyStarGel>(),
                        ModContent.ItemType<Content.Items.VoidSlime.VoidSlimeSaddle>(), ModContent.ItemType<Content.Items.VoidSlime.GalaxySolidifier>()
                    },
                    "CalamityEntropy/Assets/BCL/VoidSlime", 1f)
            };
        }
    }
}
