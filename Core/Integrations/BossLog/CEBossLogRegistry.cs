using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Integrations.BossLog
{
    /// <summary>
    /// 一条本模组图鉴条目在本侧的登记:演员(场景 + 主题)与兜底数据。
    /// 兜底只在 BossChecklist 反射面可选成员缺失时用,正常路径读它自己的 EntryInfo
    /// </summary>
    internal sealed class CEBossLogEntry
    {
        /// <summary>登记给 BossChecklist 的内部名(它按此存档与标记,不能随 NPC 类名改)</summary>
        public string InternalName { get; }
        public CEBossPortraitActor Actor { get; }
        public Func<bool> Downed { get; }
        public IReadOnlyList<int> NpcTypes { get; }

        public CEBossLogEntry(string internalName, CEBossPortraitActor actor, Func<bool> downed, IReadOnlyList<int> npcTypes) {
            InternalName = internalName;
            Actor = actor;
            Downed = downed;
            NpcTypes = npcTypes;
        }

        /// <summary>首个 NPC 的显示名(BossChecklist 单 NPC 条目的默认名就是它)</summary>
        public string FallbackName {
            get {
                if (NpcTypes.Count > 0 && ModContent.GetModNPC(NpcTypes[0]) is ModNPC npc) {
                    return npc.DisplayName.Value;
                }
                return InternalName;
            }
        }

        /// <summary>各 NPC 登记的 Boss 头图标(与 BossChecklist 的默认头图标同源)</summary>
        public List<Asset<Texture2D>> FallbackHeads() {
            List<Asset<Texture2D>> heads = [];
            foreach (int type in NpcTypes) {
                if (type < 0 || type >= NPCID.Sets.BossHeadTextures.Length) {
                    continue;
                }
                int slot = NPCID.Sets.BossHeadTextures[type];
                if (slot >= 0 && slot < TextureAssets.NpcHeadBoss.Length && TextureAssets.NpcHeadBoss[slot] != null) {
                    heads.Add(TextureAssets.NpcHeadBoss[slot]);
                }
            }
            return heads;
        }
    }

    /// <summary>
    /// 本侧图鉴条目表(键 = BossChecklist 的 <c>"模组名 内部名"</c>),
    /// 供 <see cref="CEBossLogHook"/> 判定「当前页是否本模组条目」并取主题。
    /// LogBoss 的实际登记仍在 <see cref="CEBossChecklistIntegration"/> 的条目表里,这里只挂演员
    /// </summary>
    internal static class CEBossLogRegistry
    {
        private static readonly Dictionary<string, CEBossLogEntry> entries = [];

        /// <summary>已登记条目</summary>
        public static IReadOnlyCollection<CEBossLogEntry> Entries => entries.Values;

        public static bool TryGet(string key, out CEBossLogEntry entry) {
            entry = null;
            return key != null && entries.TryGetValue(key, out entry);
        }

        /// <summary>登记一条带演员的条目;返回条目键</summary>
        public static string Add(Mod host, string internalName, CEBossPortraitActor actor, Func<bool> downed, object npcTypes) {
            string key = $"{host.Name} {internalName}";
            entries[key] = new CEBossLogEntry(internalName, actor, downed, AsTypeList(npcTypes));
            return key;
        }

        public static void Clear() => entries.Clear();

        private static IReadOnlyList<int> AsTypeList(object npcTypes) => npcTypes switch {
            List<int> list => list,
            int single => [single],
            _ => Array.Empty<int>(),
        };
    }
}
