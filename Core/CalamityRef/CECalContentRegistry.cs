using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using CalamityEntropy.Content.NPCs.SpiritFountain;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.CalamityRef
{
    /// <summary>
    /// 灾厄在场时才生效的三张注册表:强制解除灾厄 Boss 对本模组减益的免疫、
    /// 把本模组的从属部件从灾厄血条里排除、灾厄 Boss 的血条手调色。
    /// 三者都以 <see cref="CERef.Has"/> 把关,无灾厄时整段静默跳过;
    /// 原先直写在模组入口的 PostSetupContent 里。
    /// </summary>
    internal static class CECalContentRegistry
    {
        /// <summary>3.33 原表:强制 30 个灾厄 Boss 不免疫本模组三个自有减益。CEID 未命中返回 0,必须跳过</summary>
        public static void RegisterDebuffImmunityOverrides() {
            if (!CERef.Has) {
                return;
            }
            int[] specBuffs = new int[]
            {
                ModContent.BuffType<VoidVirus>(),
                ModContent.BuffType<SoulDisorder>(),
                ModContent.BuffType<Deceive>()
            };
            int[] specNpcs = new int[]
            {
                CEID.NPC_DesertScourgeHead,
                CEID.NPC_DevourerofGodsHead,
                CEID.NPC_AstrumDeusHead,
                CEID.NPC_AquaticScourgeHead,
                CEID.NPC_AstrumAureus,
                CEID.NPC_BrimstoneElemental,
                CEID.NPC_Dragonfolly,
                CEID.NPC_CalamitasClone,
                CEID.NPC_CeaselessVoid,
                CEID.NPC_Crabulon,
                CEID.NPC_Cryogen,
                CEID.NPC_CryogenShield,
                CEID.NPC_AresBody,
                CEID.NPC_Artemis,
                CEID.NPC_Apollo,
                CEID.NPC_ThanatosHead,
                CEID.NPC_GreatSandShark,
                CEID.NPC_HiveMind,
                CEID.NPC_PerforatorHive,
                CEID.NPC_Leviathan,
                CEID.NPC_Anahita,
                CEID.NPC_PlaguebringerGoliath,
                CEID.NPC_Polterghast,
                CEID.NPC_PrimordialWyrmHead,
                CEID.NPC_Providence,
                CEID.NPC_RavagerBody,
                CEID.NPC_Signus,
                CEID.NPC_StormWeaverHead,
                CEID.NPC_Yharon,
                CEID.NPC_SupremeCalamitas
            };
            foreach (int buff in specBuffs) {
                foreach (int npcType in specNpcs) {
                    if (npcType > 0) {
                        NPCID.Sets.SpecificDebuffImmunity[npcType][buff] = false;
                    }
                }
            }
        }

        //灾厄血条只看 npc.boss、完全不看 realLife,自有 Boss 的从属部件不登记排除表就一个部件一根条。
        //3.33 原有巡游者两条,另两条是同型缺陷:同样置了 boss=true 且血条无意义
        public static void RegisterBossBarExclusions() {
            if (!CERef.Has) {
                return;
            }
            //巡游者段体与尾:每帧镜像头的血量,排除后头那根条显示的就是全蠕虫真血
            CECal.ExcludeFromCalBossBar(ModContent.NPCType<CruiserBody>());
            CECal.ExcludeFromCalBossBar(ModContent.NPCType<CruiserTail>());
            //灵泉环:每帧镜像本体血量,一场至少十条
            CECal.ExcludeFromCalBossBar(ModContent.NPCType<SpiritRing>());
            //混沌细胞:伤害经 realLife 全额转给本体,自身 life 从不变动,会挂一根永远满格的死条
            CECal.ExcludeFromCalBossBar(ModContent.NPCType<ChaoticCell>());
        }

        /// <summary>3.33 手调色表 38 条。未命中的 CEID 经 SetColorIfFound 过滤,不污染 NPCID 0</summary>
        public static void RegisterBossbarColors() {
            EntropyBossbar.profanedEnrageNPCs.Clear();
            if (!CERef.Has) {
                return;
            }
            EntropyBossbar.SetColorIfFound(CEID.NPC_DesertScourgeHead, new Color(216, 210, 175));
            EntropyBossbar.SetColorIfFound(CEID.NPC_GiantClam, new Color(128, 255, 255));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Crabulon, new Color(133, 255, 237));
            EntropyBossbar.SetColorIfFound(CEID.NPC_HiveMind, new Color(140, 60, 255));
            EntropyBossbar.SetColorIfFound(CEID.NPC_PerforatorHive, new Color(155, 60, 60));
            EntropyBossbar.SetColorIfFound(CEID.NPC_CrimulanPaladin, new Color(255, 60, 75));
            EntropyBossbar.SetColorIfFound(CEID.NPC_SplitCrimulanPaladin, new Color(255, 60, 75));
            EntropyBossbar.SetColorIfFound(CEID.NPC_EbonianPaladin, new Color(160, 170, 220));
            EntropyBossbar.SetColorIfFound(CEID.NPC_SplitEbonianPaladin, new Color(160, 170, 220));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Cryogen, new Color(140, 255, 255));
            EntropyBossbar.SetColorIfFound(CEID.NPC_AquaticScourgeHead, new Color(215, 195, 155));
            EntropyBossbar.SetColorIfFound(CEID.NPC_BrimstoneElemental, new Color(255, 145, 115));
            EntropyBossbar.SetColorIfFound(CEID.NPC_CalamitasClone, new Color(255, 145, 115));
            EntropyBossbar.SetColorIfFound(CEID.NPC_GreatSandShark, new Color(225, 190, 130));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Anahita, new Color(180, 180, 230));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Leviathan, new Color(80, 235, 140));
            EntropyBossbar.SetColorIfFound(CEID.NPC_AstrumAureus, new Color(130, 130, 160));
            EntropyBossbar.SetColorIfFound(CEID.NPC_PlaguebringerGoliath, new Color(60, 160, 30));
            EntropyBossbar.SetColorIfFound(CEID.NPC_RavagerBody, new Color(190, 180, 155));
            EntropyBossbar.SetColorIfFound(CEID.NPC_AstrumDeusHead, new Color(96, 230, 190));
            EntropyBossbar.SetColorIfFound(CEID.NPC_ProfanedGuardianCommander, new Color(255, 255, 120));
            EntropyBossbar.SetColorIfFound(CEID.NPC_ProfanedGuardianDefender, new Color(255, 255, 120));
            EntropyBossbar.SetColorIfFound(CEID.NPC_ProfanedGuardianHealer, new Color(255, 255, 120));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Providence, new Color(255, 255, 120));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Dragonfolly, new Color(200, 180, 100));
            EntropyBossbar.SetColorIfFound(CEID.NPC_CeaselessVoid, new Color(180, 210, 220));
            EntropyBossbar.SetColorIfFound(CEID.NPC_StormWeaverHead, new Color(120, 145, 180));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Signus, new Color(223, 75, 170));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Polterghast, new Color(100, 255, 255));
            EntropyBossbar.SetColorIfFound(CEID.NPC_OldDuke, new Color(190, 170, 130));
            EntropyBossbar.SetColorIfFound(CEID.NPC_DevourerofGodsHead, new Color(121, 230, 255));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Yharon, new Color(255, 220, 100));
            EntropyBossbar.SetColorIfFound(CEID.NPC_AresBody, new Color(242, 112, 73));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Apollo, new Color(146, 200, 130));
            EntropyBossbar.SetColorIfFound(CEID.NPC_Artemis, new Color(146, 200, 130));
            EntropyBossbar.SetColorIfFound(CEID.NPC_ThanatosHead, new Color(135, 220, 240));
            EntropyBossbar.SetColorIfFound(CEID.NPC_SupremeCalamitas, new Color(255, 145, 115));
            EntropyBossbar.SetColorIfFound(CEID.NPC_PrimordialWyrmHead, new Color(255, 255, 80));

            AddIfFound(CEID.NPC_Providence);
            AddIfFound(CEID.NPC_ProfanedGuardianCommander);
            AddIfFound(CEID.NPC_ProfanedGuardianHealer);
            AddIfFound(CEID.NPC_ProfanedGuardianDefender);
        }

        private static void AddIfFound(int npcType) {
            if (npcType > 0) {
                EntropyBossbar.profanedEnrageNPCs.Add(npcType);
            }
        }
    }
}
