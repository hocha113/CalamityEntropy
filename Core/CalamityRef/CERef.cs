using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.CalamityRef
{
    /// <summary>灾厄反射层。只读不写;未装灾厄或反射失败一律返回空值,兜底由 CECal 负责</summary>
    internal static class CERef
    {
        private const string CalName = "CalamityMod";

        /// <summary>已装 CalamityMod(不校版本)。成员取不到时全部走空值防护</summary>
        public static bool Has
        {
            get
            {
                has ??= ModLoader.TryGetMod(CalName, out calamity);
                return has.Value;
            }
        }

        /// <summary>该 Mod 是否就是已缓存的灾厄实例。调用点不要再写模组名字符串</summary>
        internal static bool IsCalamity(Mod mod)
        {
            return Has && mod != null && ReferenceEquals(mod, calamity);
        }

        /// <summary>转调灾厄对第三方公开的 ModCall。未装灾厄返回 null;异常按本类惯例只记一次日志后吞掉。
        /// 调用点一律走 CECal 的语义方法,不要直接拼灾厄的 case 名</summary>
        internal static object Call(params object[] args)
        {
            if (!Has || calamity == null)
            {
                return null;
            }
            try
            {
                return calamity.Call(args);
            }
            catch (Exception ex)
            {
                string head = args != null && args.Length > 0 && args[0] != null ? args[0].ToString() : "?";
                LogFailed("Call", head + " " + ex.GetType().Name);
                return null;
            }
        }

        /// <summary>A4 档案馆类型。未装灾厄或反射失败为 null</summary>
        internal static Type DungeonArchiveType => dungeonArchiveType;

        /// <summary>A4 读档回填用的世界系统类型。未装灾厄或反射失败为 null</summary>
        internal static Type WorldgenManagementSystemType => worldgenManagementSystemType;

        private static bool? has;
        private static Mod calamity;

        private const BindingFlags PublicStaticFlags = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        private static readonly HashSet<string> loggedFailures = new();

        private static Type downedBossSystemType;
        private static PropertyInfo[] downedProps;
        private static FlagCache[] downedCache;

        private static MemberInfo calWorld_revenge_M;
        private static MemberInfo calWorld_death_M;
        private static MemberInfo bossRush_Active_M;
        private static FlagCache revengeCache;
        private static FlagCache deathCache;
        private static FlagCache bossRushCache;

        private static ModPlayer calPlayerTemplate;
        private static MemberInfo calPlayer_ZoneAstral_M;
        private static MemberInfo calPlayer_ZoneSulphur_M;
        private static MemberInfo calPlayer_ZoneAbyssLayer4_M;

        private static GlobalNPC calGlobalNPCTemplate;
        private static MemberInfo calNPC_CurrentlyEnraged_M;
        private static MemberInfo calNPC_CurrentlyIncreasingDefenseOrDR_M;
        private static MemberInfo calNPC_DR_M;

        private static Type dungeonArchiveType;
        private static Type worldgenManagementSystemType;

        private struct FlagCache
        {
            public uint Frame;
            public bool Value;
        }

        /// <summary>灾厄 downed 旗标下标。仅用于寻址 downedProps / downedCache,调用侧走 CECal</summary>
        internal enum DownedFlag
        {
            DesertScourge,
            HiveMind,
            Perforator,
            SlimeGod,
            Cryogen,
            AquaticScourge,
            BrimstoneElemental,
            CalamitasClone,
            Plaguebringer,
            Ravager,
            AstrumDeus,
            Dragonfolly,
            Providence,
            CeaselessVoid,
            StormWeaver,
            Signus,
            Polterghast,
            BoomerDuke,
            DoG,
            Yharon,
            ExoMechs,
            Calamitas,
            PrimordialWyrm,
            BossRush,
            Count
        }

        private static void LogFailed(string what, string fullName)
        {
            if (!loggedFailures.Add(what + "|" + fullName))
            {
                return;
            }
            CalamityEntropy inst = CalamityEntropy.Instance;
            inst?.Logger.Warn("[CERef] 反射失败 " + what + ": " + fullName);
        }

        private static Type GetModType(string fullName)
        {
            Type type = calamity?.Code.GetType(fullName);
            if (type == null)
            {
                LogFailed("Type", fullName);
            }
            return type;
        }

        private static PropertyInfo GetProperty(Type type, string name, BindingFlags flags)
        {
            if (type == null)
            {
                return null;
            }
            PropertyInfo property = type.GetProperty(name, flags);
            if (property == null)
            {
                LogFailed("Property", type.FullName + "." + name);
            }
            return property;
        }

        private static MemberInfo GetFieldOrProperty(Type type, string name, BindingFlags flags)
        {
            if (type == null)
            {
                return null;
            }
            MemberInfo member = type.GetField(name, flags);
            if (member == null)
            {
                member = type.GetProperty(name, flags);
            }
            if (member == null)
            {
                LogFailed("FieldOrProperty", type.FullName + "." + name);
            }
            return member;
        }

        private static object GetMember(MemberInfo member, object obj)
        {
            if (member == null)
            {
                return null;
            }
            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    return field.GetValue(obj);
                }
                PropertyInfo property = member as PropertyInfo;
                if (property != null)
                {
                    return property.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
                LogFailed("GetValue", (member.DeclaringType != null ? member.DeclaringType.FullName : "?") + "." + member.Name + " " + ex.GetType().Name);
            }
            return null;
        }

        private static bool ReadCachedFlag(MemberInfo member, ref FlagCache cache)
        {
            if (member == null)
            {
                return false;
            }
            if (cache.Frame != Main.GameUpdateCount)
            {
                cache.Frame = Main.GameUpdateCount;
                object raw = GetMember(member, null);
                cache.Value = raw is bool flag && flag;
            }
            return cache.Value;
        }

        /// <summary>读灾厄 downed 旗标。一帧内至多反射一次;未装灾厄或反射失败恒 false</summary>
        public static bool GetDowned(DownedFlag flag)
        {
            if (downedProps == null)
            {
                return false;
            }
            int index = (int)flag;
            if (index < 0 || index >= downedProps.Length)
            {
                return false;
            }
            PropertyInfo prop = downedProps[index];
            if (prop == null)
            {
                return false;
            }
            ref FlagCache cache = ref downedCache[index];
            if (cache.Frame != Main.GameUpdateCount)
            {
                cache.Frame = Main.GameUpdateCount;
                object raw = prop.GetValue(null);
                cache.Value = raw is bool flagValue && flagValue;
            }
            return cache.Value;
        }

        public static bool GetRevengeance()
        {
            return ReadCachedFlag(calWorld_revenge_M, ref revengeCache);
        }

        public static bool GetDeathMode()
        {
            return ReadCachedFlag(calWorld_death_M, ref deathCache);
        }

        public static bool GetBossRushActive()
        {
            return ReadCachedFlag(bossRush_Active_M, ref bossRushCache);
        }

        private static ModPlayer GetCalPlayer(Player player)
        {
            if (calPlayerTemplate == null || player == null)
            {
                return null;
            }
            if (!player.TryGetModPlayer(calPlayerTemplate, out ModPlayer calPlayer))
            {
                return null;
            }
            return calPlayer;
        }

        private static bool GetPlayerFlag(Player player, MemberInfo member)
        {
            ModPlayer calPlayer = GetCalPlayer(player);
            if (calPlayer == null || member == null)
            {
                return false;
            }
            object raw = GetMember(member, calPlayer);
            return raw is bool flag && flag;
        }

        public static bool GetZoneAstral(Player player)
        {
            return GetPlayerFlag(player, calPlayer_ZoneAstral_M);
        }

        public static bool GetZoneSulphur(Player player)
        {
            return GetPlayerFlag(player, calPlayer_ZoneSulphur_M);
        }

        public static bool GetZoneAbyssLayer4(Player player)
        {
            return GetPlayerFlag(player, calPlayer_ZoneAbyssLayer4_M);
        }

        private static GlobalNPC GetCalGlobalNPC(NPC npc)
        {
            if (calGlobalNPCTemplate == null || npc == null)
            {
                return null;
            }
            if (!npc.TryGetGlobalNPC(calGlobalNPCTemplate, out GlobalNPC calNpc))
            {
                return null;
            }
            return calNpc;
        }

        private static bool GetNpcFlag(NPC npc, MemberInfo member)
        {
            GlobalNPC calNpc = GetCalGlobalNPC(npc);
            if (calNpc == null || member == null)
            {
                return false;
            }
            object raw = GetMember(member, calNpc);
            return raw is bool flag && flag;
        }

        public static bool GetNPCEnraged(NPC npc)
        {
            return GetNpcFlag(npc, calNPC_CurrentlyEnraged_M);
        }

        public static bool GetNPCIncreasingDefenseOrDR(NPC npc)
        {
            return GetNpcFlag(npc, calNPC_CurrentlyIncreasingDefenseOrDR_M);
        }

        public static float GetNPCDR(NPC npc)
        {
            GlobalNPC calNpc = GetCalGlobalNPC(npc);
            if (calNpc == null || calNPC_DR_M == null)
            {
                return 0f;
            }
            object raw = GetMember(calNPC_DR_M, calNpc);
            return raw is float value ? value : 0f;
        }

        internal static void LoadData()
        {
            has = ModLoader.TryGetMod(CalName, out calamity);
            if (!has.Value)
            {
                return;
            }

            //命名空间是 CalamityMod,不是按目录猜的 CalamityMod.World / Systems.ProgressionAndEvent
            downedBossSystemType = GetModType("CalamityMod.DownedBossSystem");
            int flagCount = (int)DownedFlag.Count;
            downedProps = new PropertyInfo[flagCount];
            downedCache = new FlagCache[flagCount];
            if (downedBossSystemType != null)
            {
                for (int i = 0; i < flagCount; i++)
                {
                    string propName = "downed" + ((DownedFlag)i).ToString();
                    downedProps[i] = GetProperty(downedBossSystemType, propName, PublicStaticFlags);
                    downedCache[i].Frame = uint.MaxValue;
                }
            }

            Type calWorld = GetModType("CalamityMod.World.CalamityWorld");
            calWorld_revenge_M = GetFieldOrProperty(calWorld, "revenge", PublicStaticFlags);
            calWorld_death_M = GetFieldOrProperty(calWorld, "death", PublicStaticFlags);

            Type bossRush = GetModType("CalamityMod.Events.BossRushEvent");
            bossRush_Active_M = GetFieldOrProperty(bossRush, "BossRushActive", PublicStaticFlags);

            dungeonArchiveType = GetModType("CalamityMod.World.DungeonArchive");
            worldgenManagementSystemType = GetModType("CalamityMod.Systems.WorldgenManagementSystem");

            revengeCache.Frame = uint.MaxValue;
            deathCache.Frame = uint.MaxValue;
            bossRushCache.Frame = uint.MaxValue;
        }

        internal static void SetupData()
        {
            if (Has)
            {
                if (!ModContent.TryFind(CalName, "CalamityPlayer", out calPlayerTemplate))
                {
                    LogFailed("TryFind", "CalamityMod/CalamityPlayer");
                }
                else
                {
                    Type calPlayerType = calPlayerTemplate.GetType();
                    calPlayer_ZoneAstral_M = GetFieldOrProperty(calPlayerType, "ZoneAstral", PublicInstanceFlags);
                    calPlayer_ZoneSulphur_M = GetFieldOrProperty(calPlayerType, "ZoneSulphur", PublicInstanceFlags);
                    calPlayer_ZoneAbyssLayer4_M = GetFieldOrProperty(calPlayerType, "ZoneAbyssLayer4", PublicInstanceFlags);
                }

                if (!ModContent.TryFind(CalName, "CalamityGlobalNPC", out calGlobalNPCTemplate))
                {
                    LogFailed("TryFind", "CalamityMod/CalamityGlobalNPC");
                }
                else
                {
                    Type calNpcType = calGlobalNPCTemplate.GetType();
                    calNPC_CurrentlyEnraged_M = GetFieldOrProperty(calNpcType, "CurrentlyEnraged", PublicInstanceFlags);
                    calNPC_CurrentlyIncreasingDefenseOrDR_M = GetFieldOrProperty(calNpcType, "CurrentlyIncreasingDefenseOrDR", PublicInstanceFlags);
                    calNPC_DR_M = GetFieldOrProperty(calNpcType, "DR", PublicInstanceFlags);
                }
            }

            CEID.SealMissCache();
        }

        internal static void UnLoadData()
        {
            has = null;
            calamity = null;
            loggedFailures.Clear();

            downedBossSystemType = null;
            downedProps = null;
            downedCache = null;

            calWorld_revenge_M = null;
            calWorld_death_M = null;
            bossRush_Active_M = null;
            revengeCache = default;
            deathCache = default;
            bossRushCache = default;

            calPlayerTemplate = null;
            calPlayer_ZoneAstral_M = null;
            calPlayer_ZoneSulphur_M = null;
            calPlayer_ZoneAbyssLayer4_M = null;

            calGlobalNPCTemplate = null;
            calNPC_CurrentlyEnraged_M = null;
            calNPC_CurrentlyIncreasingDefenseOrDR_M = null;
            calNPC_DR_M = null;

            dungeonArchiveType = null;
            worldgenManagementSystemType = null;

            CEID.UnLoadData();
        }
    }

    /// <summary>把 CERef 接到 ICELoader。本类不得有任何字段</summary>
    internal class CERefLoader : ICELoader
    {
        public void LoadData()
        {
            CERef.LoadData();
        }

        public void SetupData()
        {
            CERef.SetupData();
        }

        public void UnLoadData()
        {
            CERef.UnLoadData();
        }
    }
}
