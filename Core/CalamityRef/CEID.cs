using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.CalamityRef
{
    /// <summary>灾厄内容 ID 按名解析。属性名即灾厄内部名,前缀决定内容类别。未命中恒返回 0</summary>
    internal static class CEID
    {
        private const string CalName = "CalamityMod";

        private static readonly Dictionary<string, int> idCache = new();
        private static readonly HashSet<string> loggedKeys = new();
        //PostSetupContent 之前不缓存未命中,免得把「灾厄内容尚未注册」误封成永久缺失
        private static bool cacheMisses;

        #region 物品
        public static int Item_Abaddon => Get();
        public static int Item_AbyssBlade => Get();
        public static int Item_AbyssShellFossil => Get();
        public static int Item_AbyssalDivingGear => Get();
        public static int Item_AbyssalDivingSuit => Get();
        public static int Item_AbyssalTreasure => Get();
        public static int Item_AegisBlade => Get();
        public static int Item_AerialiteBar => Get();
        public static int Item_AlphaDraconis => Get();
        public static int Item_AltarOfTheAccursedItem => Get();
        public static int Item_AncientBoneDust => Get();
        public static int Item_AnechoicCoating => Get();
        public static int Item_AngelicShotgun => Get();
        public static int Item_AquaticScourgeBag => Get();
        public static int Item_ArkoftheCosmos => Get();
        public static int Item_ArkoftheElements => Get();
        public static int Item_ArmoredShell => Get();
        public static int Item_AscendantInsignia => Get();
        public static int Item_AscendantSpiritEssence => Get();
        public static int Item_AsgardianAegis => Get();
        public static int Item_AshenStalactite => Get();
        public static int Item_AshesofAnnihilation => Get();
        public static int Item_AshesofCalamity => Get();
        public static int Item_AstralBar => Get();
        public static int Item_AstrealDefeat => Get();
        public static int Item_AstrumAureusBag => Get();
        public static int Item_AstrumDeusBag => Get();
        public static int Item_Auralis => Get();
        public static int Item_AureusCell => Get();
        public static int Item_AuricBar => Get();
        public static int Item_AuricOre => Get();
        public static int Item_AuricToilet => Get();
        public static int Item_Barinade => Get();
        public static int Item_Barinautical => Get();
        public static int Item_BelchingSaxophone => Get();
        public static int Item_BlightedGel => Get();
        public static int Item_BloodOrb => Get();
        public static int Item_Bloodstone => Get();
        public static int Item_BloomStone => Get();
        public static int Item_BlossomFlux => Get();
        public static int Item_BlossomPickaxe => Get();
        public static int Item_BobbitHook => Get();
        public static int Item_BotanicChair => Get();
        public static int Item_BrimstoneElementalBag => Get();
        public static int Item_BrimstoneSword => Get();
        public static int Item_BrinyBaron => Get();
        public static int Item_BurntSienna => Get();
        public static int Item_CalamarisLament => Get();
        public static int Item_CalamitasCloneBag => Get();
        public static int Item_CalamitasCoffer => Get();
        public static int Item_CeaselessVoidBag => Get();
        public static int Item_CelestialClaymore => Get();
        public static int Item_ChaliceOfTheBloodGod => Get();
        public static int Item_ChickenCannon => Get();
        public static int Item_Cinderplate => Get();
        public static int Item_CleansingBlaze => Get();
        public static int Item_ClockworkBow => Get();
        public static int Item_CodebreakerBase => Get();
        public static int Item_ContinentalGreatbow => Get();
        public static int Item_CoreofCalamity => Get();
        public static int Item_CorrodedFossil => Get();
        public static int Item_CosmicDischarge => Get();
        public static int Item_CosmicViperEngine => Get();
        public static int Item_CosmiliteBar => Get();
        public static int Item_CosmiliteChair => Get();
        public static int Item_CrabulonBag => Get();
        public static int Item_CrescentMoon => Get();
        public static int Item_CrownJewel => Get();
        public static int Item_CryoStone => Get();
        public static int Item_CryogenBag => Get();
        public static int Item_CryonicBar => Get();
        public static int Item_DarkPlasma => Get();
        public static int Item_DarkSunRing => Get();
        public static int Item_DarksunFragment => Get();
        public static int Item_DazzlingStabberStaff => Get();
        public static int Item_DeathsAscension => Get();
        public static int Item_DeepSeaDumbbell => Get();
        public static int Item_DefiledGreatsword => Get();
        public static int Item_DepthCells => Get();
        public static int Item_DesertScourgeBag => Get();
        public static int Item_DevilsDevastation => Get();
        public static int Item_DevourerofGodsBag => Get();
        public static int Item_DimensionTearingDisk => Get();
        public static int Item_DivineGeode => Get();
        public static int Item_DraedonBag => Get();
        public static int Item_DragonPow => Get();
        public static int Item_DragonsBreath => Get();
        public static int Item_Drataliornus => Get();
        public static int Item_DubiousPlating => Get();
        public static int Item_EffulgentFeather => Get();
        public static int Item_EidolicWail => Get();
        public static int Item_EidolonStaff => Get();
        public static int Item_ElephantKiller => Get();
        public static int Item_EndoHydraStaff => Get();
        public static int Item_EndothermicEnergy => Get();
        public static int Item_EnergyCore => Get();
        public static int Item_EssenceofHavoc => Get();
        public static int Item_EssenceofSunlight => Get();
        public static int Item_EternalBlizzard => Get();
        public static int Item_ExaltedOathblade => Get();
        public static int Item_ExoPrism => Get();
        public static int Item_ExodiumCluster => Get();
        public static int Item_FantasyTalisman => Get();
        public static int Item_FlarefrostBlade => Get();
        public static int Item_Floodtide => Get();
        public static int Item_FrigidflashBolt => Get();
        public static int Item_GalactusBlade => Get();
        public static int Item_GalaxySmasher => Get();
        public static int Item_GiantShell => Get();
        public static int Item_GlacialEmbrace => Get();
        public static int Item_GrandDad => Get();
        public static int Item_GrandScale => Get();
        public static int Item_HadalUrn => Get();
        public static int Item_HalibutCannon => Get();
        public static int Item_Heresy => Get();
        public static int Item_HiveMindBag => Get();
        public static int Item_HivePod => Get();
        public static int Item_HydrothermalCrate => Get();
        public static int Item_HyperdeathRiftScepter => Get();
        public static int Item_IceBarrage => Get();
        public static int Item_IceStar => Get();
        public static int Item_IcicleTrident => Get();
        public static int Item_IgneousExaltation => Get();
        public static int Item_InfectedArmorPlating => Get();
        public static int Item_JawsOfOblivion => Get();
        public static int Item_Keelhaul => Get();
        public static int Item_Kingsbane => Get();
        public static int Item_LeviathanBag => Get();
        public static int Item_LifeAlloy => Get();
        public static int Item_LivingShard => Get();
        public static int Item_LoreAbyss => Get();
        public static int Item_LoreAquaticScourge => Get();
        public static int Item_LoreArchmage => Get();
        public static int Item_LoreAstralInfection => Get();
        public static int Item_LoreAstrumAureus => Get();
        public static int Item_LoreAstrumDeus => Get();
        public static int Item_LoreAwakening => Get();
        public static int Item_LoreAzafure => Get();
        public static int Item_LoreBloodMoon => Get();
        public static int Item_LoreBrainofCthulhu => Get();
        public static int Item_LoreBrimstoneElemental => Get();
        public static int Item_LoreCalamitasClone => Get();
        public static int Item_LoreCorruption => Get();
        public static int Item_LoreCrabulon => Get();
        public static int Item_LoreCrimson => Get();
        public static int Item_LoreDesertScourge => Get();
        public static int Item_LoreDestroyer => Get();
        public static int Item_LoreDukeFishron => Get();
        public static int Item_LoreEaterofWorlds => Get();
        public static int Item_LoreEmpressofLight => Get();
        public static int Item_LoreEyeofCthulhu => Get();
        public static int Item_LoreGolem => Get();
        public static int Item_LoreHiveMind => Get();
        public static int Item_LoreKingSlime => Get();
        public static int Item_LoreLeviathanAnahita => Get();
        public static int Item_LoreMechs => Get();
        public static int Item_LorePerforators => Get();
        public static int Item_LorePlaguebringerGoliath => Get();
        public static int Item_LorePlantera => Get();
        public static int Item_LorePrelude => Get();
        public static int Item_LoreQueenBee => Get();
        public static int Item_LoreQueenSlime => Get();
        public static int Item_LoreRavager => Get();
        public static int Item_LoreSkeletron => Get();
        public static int Item_LoreSkeletronPrime => Get();
        public static int Item_LoreSlimeGod => Get();
        public static int Item_LoreSulphurSea => Get();
        public static int Item_LoreTwins => Get();
        public static int Item_LoreUnderworld => Get();
        public static int Item_LoreWallofFlesh => Get();
        public static int Item_Lumenyl => Get();
        public static int Item_Malachite => Get();
        public static int Item_ManaPolarizer => Get();
        public static int Item_MawOfInfinity => Get();
        public static int Item_MeldBlob => Get();
        public static int Item_MiracleMatter => Get();
        public static int Item_MirrorBlade => Get();
        public static int Item_MirrorofKalandra => Get();
        public static int Item_MoltenAmputator => Get();
        public static int Item_MoonstoneCrown => Get();
        public static int Item_Murasama => Get();
        public static int Item_MysteriousCircuitry => Get();
        public static int Item_Nadir => Get();
        public static int Item_Necroplasm => Get();
        public static int Item_NightmareFuel => Get();
        public static int Item_Norfleet => Get();
        public static int Item_OccultSkullCrown => Get();
        public static int Item_OldLordClaymore => Get();
        public static int Item_OmegaBlueChestplate => Get();
        public static int Item_OmegaBlueHelmet => Get();
        public static int Item_OmegaBlueTentacles => Get();
        public static int Item_OmegaHealingPotion => Get();
        public static int Item_Omicron => Get();
        public static int Item_Onyxia => Get();
        public static int Item_OverloadedSludge => Get();
        public static int Item_P90 => Get();
        public static int Item_PearlShard => Get();
        public static int Item_PerennialBar => Get();
        public static int Item_PerforatorBag => Get();
        public static int Item_PhantasmalRuin => Get();
        public static int Item_PlagueCellCanister => Get();
        public static int Item_PlaguebringerGoliathBag => Get();
        public static int Item_PlanetaryAnnihilation => Get();
        public static int Item_PlantyMush => Get();
        public static int Item_PlasmaRod => Get();
        public static int Item_PoleWarper => Get();
        public static int Item_PolterghastBag => Get();
        public static int Item_Poseidon => Get();
        public static int Item_PrimordialEarth => Get();
        public static int Item_PrismShard => Get();
        public static int Item_ProvidenceBag => Get();
        public static int Item_PurifiedGel => Get();
        public static int Item_Radiance => Get();
        public static int Item_RampartofDeities => Get();
        public static int Item_ReaperTooth => Get();
        public static int Item_ReaperToothNecklace => Get();
        public static int Item_Regenerator => Get();
        public static int Item_Riftburst => Get();
        public static int Item_Rock => Get();
        public static int Item_RogueEmblem => Get();
        public static int Item_RottenDogtooth => Get();
        public static int Item_RoverDrive => Get();
        public static int Item_RuinMedallion => Get();
        public static int Item_RuinousSoul => Get();
        public static int Item_RustyBeaconPrototype => Get();
        public static int Item_SaharaSlicers => Get();
        public static int Item_SamsaraSlicer => Get();
        public static int Item_SanctifiedSpark => Get();
        public static int Item_ScoriaBar => Get();
        public static int Item_SeaPrism => Get();
        public static int Item_SeashineSword => Get();
        public static int Item_SeraphTracers => Get();
        public static int Item_SilvaChair => Get();
        public static int Item_SkyfinBombers => Get();
        public static int Item_SlimeGodBag => Get();
        public static int Item_SmoothVoidstone => Get();
        public static int Item_SolarVeil => Get();
        public static int Item_SparkSpreader => Get();
        public static int Item_Spyker => Get();
        public static int Item_StarSputter => Get();
        public static int Item_StarblightSoot => Get();
        public static int Item_StarterBag => Get();
        public static int Item_StatisNinjaBelt => Get();
        public static int Item_StormSaber => Get();
        public static int Item_StormWeaverBag => Get();
        public static int Item_StormjawStaff => Get();
        public static int Item_StormlionMandible => Get();
        public static int Item_SubductionSlicer => Get();
        public static int Item_SulphuricScale => Get();
        public static int Item_SulphurousSand => Get();
        public static int Item_SuperradiantSlaughterer => Get();
        public static int Item_SuspiciousScrap => Get();
        public static int Item_Swordsplosion => Get();
        public static int Item_TelluricGlare => Get();
        public static int Item_TenebreusTides => Get();
        public static int Item_TerrorBlade => Get();
        public static int Item_TheAbsorber => Get();
        public static int Item_TheBallista => Get();
        public static int Item_TheBurningSky => Get();
        public static int Item_TheCommunity => Get();
        public static int Item_TheDarkMaster => Get();
        public static int Item_TheFirstShadowflame => Get();
        public static int Item_TheHive => Get();
        public static int Item_TitanArm => Get();
        public static int Item_TitanHeart => Get();
        public static int Item_TwistingNether => Get();
        public static int Item_UltraLiquidator => Get();
        public static int Item_UnholyCore => Get();
        public static int Item_UnholyEssence => Get();
        public static int Item_UniversalGenesis => Get();
        public static int Item_UrchinStinger => Get();
        public static int Item_UrsaSergeant => Get();
        public static int Item_Valediction => Get();
        public static int Item_Vesuvius => Get();
        public static int Item_VoidCondenser => Get();
        public static int Item_VoidEaterMarionette => Get();
        public static int Item_VoidEdge => Get();
        public static int Item_VoidTorch => Get();
        public static int Item_Voidstone => Get();
        public static int Item_WindBlade => Get();
        public static int Item_WingsofRebirth => Get();
        public static int Item_Wrathwing => Get();
        public static int Item_WulfrumMetalScrap => Get();
        public static int Item_YharimsCrystal => Get();
        public static int Item_YharonBag => Get();
        public static int Item_YharonSoulFragment => Get();
        public static int Item_YharonsKindleStaff => Get();
        #endregion

        #region NPC
        public static int NPC_Anahita => Get();
        public static int NPC_Apollo => Get();
        public static int NPC_AquaticScourgeHead => Get();
        public static int NPC_AresBody => Get();
        public static int NPC_Artemis => Get();
        public static int NPC_AstrumAureus => Get();
        public static int NPC_AstrumDeusHead => Get();
        public static int NPC_BrimstoneElemental => Get();
        public static int NPC_CalamitasClone => Get();
        public static int NPC_CannonballJellyfish => Get();
        public static int NPC_CeaselessVoid => Get();
        public static int NPC_CrabShroom => Get();
        public static int NPC_Crabulon => Get();
        public static int NPC_CrimulanPaladin => Get();
        public static int NPC_Cryogen => Get();
        public static int NPC_CryogenShield => Get();
        public static int NPC_DesertNuisanceHead => Get();
        public static int NPC_DesertNuisanceHeadYoung => Get();
        public static int NPC_DesertScourgeHead => Get();
        public static int NPC_DevilFish => Get();
        public static int NPC_DevourerofGodsBody => Get();
        public static int NPC_DevourerofGodsHead => Get();
        public static int NPC_DevourerofGodsTail => Get();
        public static int NPC_Dragonfolly => Get();
        public static int NPC_EbonianPaladin => Get();
        public static int NPC_Eidolist => Get();
        public static int NPC_EidolonWyrmHead => Get();
        public static int NPC_GiantClam => Get();
        public static int NPC_GiantSquid => Get();
        public static int NPC_GreatSandShark => Get();
        public static int NPC_HiveMind => Get();
        public static int NPC_Laserfish => Get();
        public static int NPC_Leviathan => Get();
        public static int NPC_LuminousCorvina => Get();
        public static int NPC_OarfishHead => Get();
        public static int NPC_OldDuke => Get();
        public static int NPC_PerforatorHive => Get();
        public static int NPC_PlaguebringerGoliath => Get();
        public static int NPC_Polterghast => Get();
        public static int NPC_PrimordialWyrmHead => Get();
        public static int NPC_ProfanedGuardianCommander => Get();
        public static int NPC_ProfanedGuardianDefender => Get();
        public static int NPC_ProfanedGuardianHealer => Get();
        public static int NPC_Providence => Get();
        public static int NPC_RavagerBody => Get();
        public static int NPC_Signus => Get();
        public static int NPC_SlimeGodCore => Get();
        public static int NPC_SplitCrimulanPaladin => Get();
        public static int NPC_SplitEbonianPaladin => Get();
        public static int NPC_StormWeaverHead => Get();
        public static int NPC_Sulflounder => Get();
        public static int NPC_SupremeCalamitas => Get();
        public static int NPC_ThanatosHead => Get();
        public static int NPC_ToxicMinnow => Get();
        public static int NPC_Toxicatfish => Get();
        public static int NPC_Trasher => Get();
        public static int NPC_Viperfish => Get();
        public static int NPC_Yharon => Get();
        #endregion

        #region 物块
        public static int Tile_AbyssTreasureChest => Get();
        public static int Tile_CosmicAnvil => Get();
        public static int Tile_DraedonsForge => Get();
        public static int Tile_ProfanedCrucible => Get();
        public static int Tile_SCalAltarLarge => Get();
        public static int Tile_VoidCondenser => Get();
        #endregion

        #region 弹幕
        public static int Proj_BrimstoneBarrage => Get();
        public static int Proj_EidolicWailSoundwave => Get();
        public static int Proj_IceBlast => Get();
        public static int Proj_IceBomb => Get();
        #endregion

        #region 减益
        public static int Buff_AstralInjectionBuff => Get();
        public static int Buff_Mushy => Get();
        public static int Buff_Plague => Get();
        #endregion

        #region 稀有度
        public static int Rarity_BurnishedAuric => Get();
        public static int Rarity_CalamityRed => Get();
        public static int Rarity_CosmicPurple => Get();
        public static int Rarity_DarkOrange => Get();
        public static int Rarity_ExoticRainbow => Get();
        public static int Rarity_HotPink => Get();
        public static int Rarity_PureGreen => Get();
        public static int Rarity_Turquoise => Get();
        #endregion

        private static int Get([CallerMemberName] string name = "")
        {
            if (idCache.TryGetValue(name, out int cached))
            {
                return cached;
            }

            int split = name.IndexOf('_');
            if (split <= 0 || split >= name.Length - 1)
            {
                LogBadKey(name);
                return 0;
            }

            string prefix = name.Substring(0, split);
            string typeName = name.Substring(split + 1);
            int result = 0;
            bool found = false;

            switch (prefix)
            {
                case "Item":
                    if (ModContent.TryFind(CalName, typeName, out ModItem modItem))
                    {
                        result = modItem.Type;
                        found = true;
                    }
                    break;
                case "NPC":
                    if (ModContent.TryFind(CalName, typeName, out ModNPC modNPC))
                    {
                        result = modNPC.Type;
                        found = true;
                    }
                    break;
                case "Proj":
                    if (ModContent.TryFind(CalName, typeName, out ModProjectile modProj))
                    {
                        result = modProj.Type;
                        found = true;
                    }
                    break;
                case "Tile":
                    if (ModContent.TryFind(CalName, typeName, out ModTile modTile))
                    {
                        result = modTile.Type;
                        found = true;
                    }
                    break;
                case "Buff":
                    if (ModContent.TryFind(CalName, typeName, out ModBuff modBuff))
                    {
                        result = modBuff.Type;
                        found = true;
                    }
                    break;
                case "Rarity":
                    if (ModContent.TryFind(CalName, typeName, out ModRarity modRarity))
                    {
                        result = modRarity.Type;
                        found = true;
                    }
                    break;
                default:
                    LogBadKey(name);
                    return 0;
            }

            if (found || cacheMisses)
            {
                idCache[name] = result;
            }
            if (!found)
            {
                LogMiss(prefix, typeName);
            }
            return result;
        }

        private static void LogBadKey(string name)
        {
            if (!loggedKeys.Add("bad|" + name))
            {
                return;
            }
            CalamityEntropy inst = CalamityEntropy.Instance;
            inst?.Logger.Warn("[CEID] 非法属性名: " + name);
        }

        private static void LogMiss(string prefix, string typeName)
        {
            //无灾厄时未命中是预期行为,不打日志
            if (!ModLoader.TryGetMod(CalName, out _))
            {
                return;
            }
            string key = prefix + "|" + typeName;
            if (!loggedKeys.Add(key))
            {
                return;
            }
            CalamityEntropy inst = CalamityEntropy.Instance;
            inst?.Logger.Warn("[CEID] 未命中 " + prefix + ": CalamityMod/" + typeName);
        }

        internal static void SealMissCache()
        {
            cacheMisses = true;
        }

        internal static void UnLoadData()
        {
            idCache.Clear();
            loggedKeys.Clear();
            cacheMisses = false;
        }
    }
}
