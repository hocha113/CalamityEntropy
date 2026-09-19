using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityEntropy.Core.Integrations.BossLog
{
    /// <summary>
    /// BossChecklist 图鉴 UI 的反射面(对照上游 1.4.4 分支源码)。
    /// 必需项缺一即 <see cref="Resolve"/> 失败、整本书接管停用;可选项缺失各自退回兜底值,
    /// 不拦加载也不抛。全部成员在 <see cref="Clear"/> 后归零
    /// </summary>
    internal static class CEBossLogReflect
    {
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static bool Ready { get; private set; }

        /// <summary>BossChecklist.UIElements.BossLogUIElements+LogPanel.Draw(SpriteBatch)</summary>
        public static MethodInfo PanelDraw { get; private set; }
        /// <summary>BossChecklist.BossLogUI.Draw(SpriteBatch)</summary>
        public static MethodInfo LogDraw { get; private set; }

        private static PropertyInfo systemInstance;
        private static FieldInfo systemBossLog;
        private static FieldInfo hoverText;
        private static FieldInfo hoverParams;
        private static FieldInfo hoverColor;

        private static PropertyInfo logVisible;
        private static PropertyInfo logPageNum;
        private static PropertyInfo logEntry;
        private static FieldInfo bookArea;
        private static FieldInfo leftPage;
        private static FieldInfo rightPage;
        private static MethodInfo maskBoss;

        private static PropertyInfo panelId;

        private static PropertyInfo entryKey;
        private static PropertyInfo entryDisplayName;
        private static PropertyInfo entryDowned;
        private static PropertyInfo entryMarked;
        private static FieldInfo entryHeads;

        private static Action<UIElement, SpriteBatch> drawChildren;

        /// <summary>解析全部成员;返回 false 时 <paramref name="missing"/> 列出缺失的必需项</summary>
        public static bool Resolve(Mod bossChecklist, out string missing) {
            Clear();
            Assembly asm = bossChecklist?.Code;
            if (asm == null) {
                missing = "assembly";
                return false;
            }

            Type sysT = asm.GetType("BossChecklist.Systems.BossLogSystem");
            Type logT = asm.GetType("BossChecklist.BossLogUI");
            Type panelT = asm.GetType("BossChecklist.UIElements.BossLogUIElements+LogPanel");
            Type elemT = asm.GetType("BossChecklist.UIElements.BossLogUIElements+LogUIElement");
            Type entryT = asm.GetType("BossChecklist.EntryInfo");

            systemInstance = sysT?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            systemBossLog = sysT?.GetField("BossLog", Any);
            hoverText = sysT?.GetField("UIHoverText", Any);
            hoverParams = sysT?.GetField("UIHoverTextParams", Any);
            hoverColor = sysT?.GetField("UIHoverTextColor", Any);

            LogDraw = logT?.GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                [typeof(SpriteBatch)]);
            logVisible = logT?.GetProperty("BossLogVisible", Any);
            logPageNum = logT?.GetProperty("PageNum", Any);
            logEntry = logT?.GetProperty("GetLogEntryInfo", Any);
            bookArea = logT?.GetField("BookArea", Any);
            leftPage = logT?.GetField("LeftPage", Any);
            rightPage = logT?.GetField("RightPage", Any);
            maskBoss = logT?.GetMethod("MaskBoss", BindingFlags.Public | BindingFlags.Static);

            //只钩 LogPanel 自己声明的重写,别落到基类 Draw 上
            PanelDraw = panelT?.GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                [typeof(SpriteBatch)]);
            panelId = elemT?.GetProperty("Id", Any);

            entryKey = entryT?.GetProperty("Key", Any);
            entryDisplayName = entryT?.GetProperty("DisplayName", Any);
            entryDowned = entryT?.GetProperty("IsAutoDownedOrMarked", Any);
            entryMarked = entryT?.GetProperty("MarkedAsDowned", Any);
            entryHeads = entryT?.GetField("headIconTextures", Any);

            MethodInfo dc = typeof(UIElement).GetMethod("DrawChildren", BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(SpriteBatch)]);
            if (dc != null) {
                try {
                    drawChildren = (Action<UIElement, SpriteBatch>)Delegate.CreateDelegate(typeof(Action<UIElement, SpriteBatch>), dc);
                } catch (Exception) {
                    drawChildren = null;
                }
            }

            List<string> lack = [];
            Require(lack, systemInstance, "BossLogSystem.Instance");
            Require(lack, systemBossLog, "BossLogSystem.BossLog");
            Require(lack, LogDraw, "BossLogUI.Draw");
            Require(lack, logVisible, "BossLogUI.BossLogVisible");
            Require(lack, logPageNum, "BossLogUI.PageNum");
            Require(lack, logEntry, "BossLogUI.GetLogEntryInfo");
            Require(lack, PanelDraw, "LogPanel.Draw");
            Require(lack, panelId, "LogUIElement.Id");
            Require(lack, entryKey, "EntryInfo.Key");
            Require(lack, drawChildren, "UIElement.DrawChildren");

            Ready = lack.Count == 0;
            missing = string.Join(", ", lack);
            if (!Ready) {
                Clear();
            }
            return Ready;
        }

        private static void Require(List<string> lack, object member, string name) {
            if (member == null) {
                lack.Add(name);
            }
        }

        public static void Clear() {
            Ready = false;
            PanelDraw = LogDraw = maskBoss = null;
            systemInstance = logVisible = logPageNum = logEntry = panelId = null;
            entryKey = entryDisplayName = entryDowned = entryMarked = null;
            systemBossLog = hoverText = hoverParams = hoverColor = bookArea = leftPage = rightPage = entryHeads = null;
            drawChildren = null;
        }

        //==================== 读取 ====================

        /// <summary>BossLogSystem.Instance.BossLog(BossLogUI 实例),未就绪返回 null</summary>
        public static object LogUI() {
            object sys = systemInstance?.GetValue(null);
            return sys == null ? null : systemBossLog?.GetValue(sys);
        }

        public static bool LogVisible(object logUi) => logUi != null && logVisible?.GetValue(logUi) is true;

        public static int PageNum(object logUi) => logUi != null && logPageNum?.GetValue(logUi) is int n ? n : -1;

        /// <summary>当前页的 EntryInfo(非条目页返回 null)</summary>
        public static object CurrentEntry(object logUi) => logUi == null ? null : logEntry?.GetValue(logUi);

        public static string EntryKey(object entry) => entry == null ? null : entryKey?.GetValue(entry) as string;

        /// <summary>条目显示名;成员缺失返回 null,由调用方兜底</summary>
        public static string EntryDisplayName(object entry) => entry == null ? null : entryDisplayName?.GetValue(entry) as string;

        /// <summary>自动判定或手动标记为已击败;成员缺失返回 null</summary>
        public static bool? EntryDowned(object entry)
            => entry != null && entryDowned?.GetValue(entry) is bool b ? b : null;

        public static bool EntryMarked(object entry)
            => entry != null && entryMarked?.GetValue(entry) is true;

        /// <summary>条目头图标列表;成员缺失或委托为空返回 null</summary>
        public static IReadOnlyList<Asset<Texture2D>> EntryHeads(object entry) {
            if (entry == null || entryHeads?.GetValue(entry) is not Delegate factory) {
                return null;
            }
            try {
                return factory.DynamicInvoke() as IReadOnlyList<Asset<Texture2D>>;
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>进度隐藏蒙版(黑=剪影);成员缺失按白</summary>
        public static Color MaskBoss(object entry) {
            if (entry == null || maskBoss == null) {
                return Color.White;
            }
            try {
                return maskBoss.Invoke(null, [entry]) is Color c ? c : Color.White;
            } catch (Exception) {
                return Color.White;
            }
        }

        public static UIElement BookArea(object logUi) => logUi == null ? null : bookArea?.GetValue(logUi) as UIElement;
        public static UIElement LeftPage(object logUi) => logUi == null ? null : leftPage?.GetValue(logUi) as UIElement;
        public static UIElement RightPage(object logUi) => logUi == null ? null : rightPage?.GetValue(logUi) as UIElement;

        /// <summary>LogPanel 的 Id:""=书封、"PageOne"=左页、"PageTwo"=右页</summary>
        public static string PanelId(object panel) => panel == null ? null : panelId?.GetValue(panel) as string;

        /// <summary>调 UIElement.DrawChildren(protected):只画子元素,不进 Draw 重入钩子</summary>
        public static void DrawChildren(UIElement element, SpriteBatch sb) {
            if (element != null && drawChildren != null) {
                drawChildren(element, sb);
            }
        }

        /// <summary>写 BossChecklist 的自定义悬停文案(其悬停层随后绘制并自清);成员缺失静默</summary>
        public static void SetHoverText(string key, object[] args, Color color) {
            object sys = systemInstance?.GetValue(null);
            if (sys == null || hoverText == null) {
                return;
            }
            hoverText.SetValue(sys, key);
            hoverParams?.SetValue(sys, args ?? []);
            hoverColor?.SetValue(sys, color);
        }
    }
}
