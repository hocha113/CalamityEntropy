using CalamityEntropy.Common;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.CalamityRef
{
    /// <summary>灾厄联动语义层。灾厄在场读灾厄细档,缺席则回落 4.0 的原版映射语义。禁止 using static 引入本类</summary>
    internal static class CECal
    {
        #region 进度 · 一对一

        /// <summary>荒漠灾虫(1.6)。4.0 兜底:史莱姆王</summary>
        public static bool DownedDesertScourge => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.DesertScourge)
            : NPC.downedSlimeKing;

        /// <summary>腐巢意志(3.98)。4.0 兜底:世吞或克脑</summary>
        public static bool DownedHiveMind => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.HiveMind)
            : NPC.downedBoss2;

        /// <summary>血肉宿主(3.99)。4.0 兜底:世吞或克脑</summary>
        public static bool DownedPerforator => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.Perforator)
            : NPC.downedBoss2;

        /// <summary>史莱姆之神(6.7)。4.0 兜底:烬甲秽魔</summary>
        public static bool DownedSlimeGod => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.SlimeGod)
            : EDownedBosses.downedApsychos;

        /// <summary>严寒之神(8.5)。4.0 兜底:任一机械</summary>
        public static bool DownedCryogen => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.Cryogen)
            : NPC.downedMechBossAny;

        /// <summary>水澜灾虫(9.5)。4.0 兜底:幻光星蛾</summary>
        public static bool DownedAquaticScourge => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.AquaticScourge)
            : EDownedBosses.downedLuminaris;

        /// <summary>硫火元素(10.5)。4.0 兜底:机械三王全通</summary>
        public static bool DownedBrimstoneElemental => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.BrimstoneElemental)
            : NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;

        /// <summary>瘟疫使者歌利亚(14.5)。仅 Leyla 减益阶梯消费;4.0 无此档,兜底恒 false</summary>
        public static bool DownedPlaguebringer => CERef.Has && CERef.GetDowned(CERef.DownedFlag.Plaguebringer);

        /// <summary>灾厄之影(16.5 蹂躏者)。4.0 兜底:拜月邪教徒</summary>
        public static bool DownedRavager => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.Ravager)
            : NPC.downedAncientCultist;

        /// <summary>星璇之神(17.5)。4.0 兜底:拜月邪教徒</summary>
        public static bool DownedAstrumDeus => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.AstrumDeus)
            : NPC.downedAncientCultist;

        /// <summary>Dragonfolly(18.6)。仅 HorizonssKey 计数器消费;4.0 无此档,兜底恒 false</summary>
        public static bool DownedDragonfolly => CERef.Has && CERef.GetDowned(CERef.DownedFlag.Dragonfolly);

        /// <summary>无尽虚空(19.6)。4.0 兜底:虚无双子</summary>
        public static bool DownedCeaselessVoid => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.CeaselessVoid)
            : EDownedBosses.downedNihilityTwin;

        /// <summary>风暴编织者(19.61)。4.0 兜底:虚无双子</summary>
        public static bool DownedStormWeaver => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.StormWeaver)
            : EDownedBosses.downedNihilityTwin;

        /// <summary>Signus(19.62)。4.0 兜底:虚无双子</summary>
        public static bool DownedSignus => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.Signus)
            : EDownedBosses.downedNihilityTwin;

        /// <summary>幽花(20)。4.0 兜底:虚无双子</summary>
        public static bool DownedPolterghast => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.Polterghast)
            : EDownedBosses.downedNihilityTwin;

        /// <summary>老公爵(20.5)。仅 Leyla 减益阶梯消费;4.0 无此档,兜底恒 false</summary>
        public static bool DownedBoomerDuke => CERef.Has && CERef.GetDowned(CERef.DownedFlag.BoomerDuke);

        /// <summary>渊海灾虫(23.5,灾厄未注册 BC 条目)。4.0 兜底:巡游者</summary>
        public static bool DownedPrimordialWyrm => CERef.Has
            ? CERef.GetDowned(CERef.DownedFlag.PrimordialWyrm)
            : EDownedBosses.downedCruiser;

        #endregion

        #region 进度 · 一对多

        /// <summary>灾厄之影分身(11.7)。兜底按落点:Silentpeak 传 true,其余传机械三王</summary>
        public static bool DownedCalamitasClone(bool ownFallback)
        {
            return CERef.Has ? CERef.GetDowned(CERef.DownedFlag.CalamitasClone) : ownFallback;
        }

        /// <summary>亵渎天神(19)。活点兜底虚无双子,死档回生传 false</summary>
        public static bool DownedProvidence(bool ownFallback)
        {
            return CERef.Has ? CERef.GetDowned(CERef.DownedFlag.Providence) : ownFallback;
        }

        /// <summary>神明吞噬者(21)。兜底按落点:虚无双子或巡游者,死档回生传 false</summary>
        public static bool DownedDoG(bool ownFallback)
        {
            return CERef.Has ? CERef.GetDowned(CERef.DownedFlag.DoG) : ownFallback;
        }

        /// <summary>丛林龙(22)。活点兜底巡游者,死档回生传 false</summary>
        public static bool DownedYharon(bool ownFallback)
        {
            return CERef.Has ? CERef.GetDowned(CERef.DownedFlag.Yharon) : ownFallback;
        }

        /// <summary>星流巨械(22.99)。活点兜底巡游者,死档回生传 false</summary>
        public static bool DownedExoMechs(bool ownFallback)
        {
            return CERef.Has ? CERef.GetDowned(CERef.DownedFlag.ExoMechs) : ownFallback;
        }

        /// <summary>至尊灾厄(23)。活点兜底巡游者,死档回生传 false</summary>
        public static bool DownedCalamitas(bool ownFallback)
        {
            return CERef.Has ? CERef.GetDowned(CERef.DownedFlag.Calamitas) : ownFallback;
        }

        #endregion

        #region 难度与事件

        /// <summary>复仇模式。4.0 兜底:专家</summary>
        public static bool IsRevengeance => CERef.Has ? CERef.GetRevengeance() : Main.expertMode;

        /// <summary>死亡模式。4.0 兜底:大师。注意不与 EntropyMode 叠加</summary>
        public static bool IsDeathMode => CERef.Has ? CERef.GetDeathMode() : Main.masterMode;

        /// <summary>终焉之战进行中。4.0 兜底:恒 false</summary>
        public static bool IsBossRushActive => CERef.Has && CERef.GetBossRushActive();

        /// <summary>终焉之战已通关。仅城镇飞龙彩蛋石头消费;4.0 无此档,兜底恒 false</summary>
        public static bool DownedBossRush => CERef.Has && CERef.GetDowned(CERef.DownedFlag.BossRush);

        #endregion

        #region 群系

        public static bool ZoneAstral(Player player, bool ownFallback)
        {
            return CERef.Has ? CERef.GetZoneAstral(player) : ownFallback;
        }

        public static bool ZoneSulphur(Player player, bool ownFallback)
        {
            return CERef.Has ? CERef.GetZoneSulphur(player) : ownFallback;
        }

        public static bool ZoneAbyssLayer4(Player player, bool ownFallback)
        {
            return CERef.Has ? CERef.GetZoneAbyssLayer4(player) : ownFallback;
        }

        #endregion

        #region 稀有度

        /// <summary>灾厄 BurnishedAuric。4.0 兜底由调用点给</summary>
        public static int RarityBurnishedAuric(int ownFallback)
        {
            return RarityOr(CEID.Rarity_BurnishedAuric, ownFallback);
        }

        public static int RarityTurquoise(int ownFallback)
        {
            return RarityOr(CEID.Rarity_Turquoise, ownFallback);
        }

        public static int RarityCosmicPurple(int ownFallback)
        {
            return RarityOr(CEID.Rarity_CosmicPurple, ownFallback);
        }

        public static int RarityHotPink(int ownFallback)
        {
            return RarityOr(CEID.Rarity_HotPink, ownFallback);
        }

        public static int RarityCalamityRed(int ownFallback)
        {
            return RarityOr(CEID.Rarity_CalamityRed, ownFallback);
        }

        public static int RarityPureGreen(int ownFallback)
        {
            return RarityOr(CEID.Rarity_PureGreen, ownFallback);
        }

        public static int RarityDarkOrange(int ownFallback)
        {
            return RarityOr(CEID.Rarity_DarkOrange, ownFallback);
        }

        public static int RarityExoticRainbow(int ownFallback)
        {
            return RarityOr(CEID.Rarity_ExoticRainbow, ownFallback);
        }

        //ModRarity 的 Type 从 12 起,未命中的 0 与任何真实模组稀有度都不会混淆
        private static int RarityOr(int calRarity, int ownFallback)
        {
            if (!CERef.Has || calRarity <= 0)
            {
                return ownFallback;
            }
            return calRarity;
        }

        #endregion

        #region NPC 状态

        /// <summary>灾厄 NPC 处于激怒态。4.0 兜底:false</summary>
        public static bool NPCEnraged(NPC npc)
        {
            return CERef.Has ? CERef.GetNPCEnraged(npc) : false;
        }

        /// <summary>灾厄 NPC 正在提防/提 DR。4.0 兜底:false</summary>
        public static bool NPCIncreasingDefenseOrDR(NPC npc)
        {
            return CERef.Has ? CERef.GetNPCIncreasingDefenseOrDR(npc) : false;
        }

        /// <summary>灾厄 NPC 的 DR,供血条显示。4.0 兜底:0</summary>
        public static float GetDisplayDR(NPC npc)
        {
            return CERef.Has ? CERef.GetNPCDR(npc) : 0f;
        }

        #endregion

        /// <summary>整链双注册的守卫:灾厄在场,且这一链需要的全部灾厄内容都解析到了,才走 3.33 链</summary>
        public static bool CalChainReady(params int[] calTypes)
        {
            if (!CERef.Has)
            {
                return false;
            }
            if (calTypes == null)
            {
                return true;
            }
            for (int i = 0; i < calTypes.Length; i++)
            {
                if (calTypes[i] <= 0)
                {
                    return false;
                }
            }
            return true;
        }
    }

    internal static class CECalRecipeExtensions
    {
        /// <summary>灾厄在场且该内容存在时用灾厄原料,否则用自有/原版原料。两侧数量相同</summary>
        public static Recipe AddCalOrOwn(this Recipe recipe, int calType, int ownType, int stack = 1)
        {
            if (!CERef.Has || calType <= 0)
            {
                return recipe.AddIngredient(ownType, stack);
            }
            return recipe.AddIngredient(calType, stack);
        }

        /// <summary>两侧数量不等的重载</summary>
        public static Recipe AddCalOrOwn(this Recipe recipe, int calType, int calStack, int ownType, int ownStack)
        {
            if (!CERef.Has || calType <= 0)
            {
                return recipe.AddIngredient(ownType, ownStack);
            }
            return recipe.AddIngredient(calType, calStack);
        }

        /// <summary>合成站同理</summary>
        public static Recipe AddCalTileOrOwn(this Recipe recipe, int calTileType, int ownTileType)
        {
            if (!CERef.Has || calTileType <= 0)
            {
                return recipe.AddTile(ownTileType);
            }
            return recipe.AddTile(calTileType);
        }
    }
}
