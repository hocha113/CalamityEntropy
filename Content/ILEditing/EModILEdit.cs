using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.LuminarisMoth;
using CalamityEntropy.Core.CalamityRef;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static InnoVault.GameSystem.ItemRebuildLoader;

namespace CalamityEntropy.Content.ILEditing
{
    public static class EModILEdit
    {
        public static void load()
        {
            if (ModLoader.TryGetMod("AlchemistNPCLite", out var anpc))
            {
                ANPCSupport.ANPCShopAdd.LoadHook();
            }
            var Item_Name_Get_Method = typeof(Item).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            if (Item_Name_Get_Method != null)
            {
                EModHooks.Add(Item_Name_Get_Method, On_Name_Get_Hook);
            }

            var NPC_Get_Name = typeof(NPC).GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            if (NPC_Get_Name != null)
            {
                EModHooks.Add(NPC_Get_Name, On_NPC_Get_Hook);
            }

            if (CERef.Has)
            {
                StoreForbiddenArchivePositionHook.LoadHook();
            }

            CalamityEntropy.Instance.Logger.Info("CalamityEntropy's Hook Loaded");
        }

        //脱离灾厄死代码裁决:GetNPCDRMultiply随EntropyBossbar的DR显示移除失去唯一调用点,已删
        public delegate string On_GetNPCName_get_Delegate(NPC npc);
        public static List<int> LostNPCsEntropy = new() { 454, 455, 456, 457, 458, 459, 521 };
        public static string On_NPC_Get_Hook(On_GetNPCName_get_Delegate orig, NPC npc)
        {
            string n = orig(npc);
            if (CalamityEntropy.EntropyMode)
            {
                if (npc.type == NPCID.CultistBoss || npc.type == NPCID.Golem || npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight || npc.type == NPCID.GolemHead || npc.type == NPCID.GolemHeadFree || LostNPCsEntropy.Contains(npc.type))
                    n = (Language.ActiveCulture == GameCulture.FromCultureName(GameCulture.CultureName.Chinese) ? "失心" : "Lost") + " " + n;
            }
            if (npc.ModNPC != null && npc.ModNPC is Luminaris && Main.zenithWorld)
                n = CalamityEntropy.Instance.GetLocalization("Luminariswarm").Value;
            return n;
        }
        public static string On_Name_Get_Hook(On_GetItemName_get_Delegate orig, Item item)
        {
            if (Main.gameMenu || item.ModItem == null)
                return orig(item);
            string orgName = orig.Invoke(item);
            if (item.active)
            {
                string name = orgName;
                if (EGlobalItem.GetOverrideName(item, orgName, out string NameNew))
                {
                    name = NameNew;
                }
                return name;
            }
            return orgName;
        }
    }

    //A4:3.33 用 IL 抓 PlaceArchive 局部坐标。现改为 EModHooks 后置,读灾厄已写入的 DungeonArchivePos,避免局部下标随灾厄改版漂移
    public static class StoreForbiddenArchivePositionHook
    {
        private delegate void PlaceArchiveOrig();

        public static void LoadHook()
        {
            Type archiveType = CERef.DungeonArchiveType;
            if (archiveType == null)
            {
                return;
            }
            MethodInfo method = archiveType.GetMethod("PlaceArchive", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                CalamityEntropy.Instance.Logger.Warn("[CERef] 反射失败 Method: PlaceArchive");
                return;
            }
            EModHooks.Add(method, On_PlaceArchive);
        }

        private static void On_PlaceArchive(PlaceArchiveOrig orig)
        {
            orig();
            if (CERef.Has)
            {
                CopyDungeonArchivePos();
            }
        }

        public static void CopyDungeonArchivePos()
        {
            Type sysType = CERef.WorldgenManagementSystemType;
            if (sysType == null)
            {
                return;
            }
            FieldInfo field = sysType.GetField("DungeonArchivePos", BindingFlags.Public | BindingFlags.Static);
            if (field == null)
            {
                return;
            }
            object raw = field.GetValue(null);
            if (raw is Point pos && pos != Point.Zero)
            {
                EDownedBosses.ForbiddenArchiveCenter = pos;
            }
        }
    }

    public class ForbiddenArchiveCenterSync : ModSystem
    {
        public override void OnWorldLoad()
        {
            if (EDownedBosses.ForbiddenArchiveCenter.X >= 0)
            {
                return;
            }
            if (CERef.Has)
            {
                StoreForbiddenArchivePositionHook.CopyDungeonArchivePos();
            }
        }
    }

    public static class EModHooks
    {
        private static ConcurrentDictionary<(MethodBase, Delegate), Hook> _hooks = new ConcurrentDictionary<(MethodBase, Delegate), Hook>();
        public static ConcurrentDictionary<(MethodBase, Delegate), Hook> Hooks => _hooks;
        public static Hook Add(MethodBase method, Delegate hookDelegate)
        {
            if (method == null)
            {
                CalamityEntropy.Instance.Logger.Warn($"CalamityEntropy: Error when add hook to method: The MethodBase passed in is Null");
                return null;
            }
            if (hookDelegate == null)
            {
                CalamityEntropy.Instance.Logger.Warn($"CalamityEntropy: Error when add hook to {method.Name}: The HookDelegate passed in is Null");
                return null;
            }

            Hook hook = new Hook(method, hookDelegate);

            if (!hook.IsApplied)
            {
                hook.Apply();
            }
            _hooks.TryAdd((method, hookDelegate), hook);
            return hook;
        }

        public static bool CheckHookStatus()
        {
            int hookDownNum = 0;
            foreach (var hook in _hooks.Values)
            {
                if (!hook.IsApplied)
                {
                    hookDownNum++;
                }
            }
            if (hookDownNum > 0)
            {
                return false;
            }
            return true;
        }

        public static void UnLoadData()
        {
            foreach (var hook in _hooks.Values)
            {
                if (hook == null)
                {
                    continue;
                }
                if (hook.IsApplied)
                {
                    hook.Undo();
                }
                hook.Dispose();
            }
            _hooks.Clear();
        }

    }
}
