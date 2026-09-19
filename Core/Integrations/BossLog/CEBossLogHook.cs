using CalamityEntropy.Common;
using CalamityEntropy.Content.ILEditing;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityEntropy.Core.Integrations.BossLog
{
    /// <summary>
    /// BossChecklist 图鉴整本书接管。<br/>
    /// 上游只暴露 <c>customPortrait</c> 一个绘制钩子(左页一块矩形,且画在左页按钮之后、标题之前),
    /// 这里经 <see cref="EModHooks"/> 钩两处方法把整本书的绘制权拿过来:<br/>
    /// · <c>BossLogUI.Draw</c>:前置画书后全屏氛围,后置画书缘饰件;<br/>
    /// · <c>LogPanel.Draw</c>:书封(Id 为空)与左页(PageOne)在选中本模组条目时跳过 orig 全权重绘
    /// (场景铺满整页 → 子元素按钮回到场景之上 → 我们的标题块),右页(PageTwo)原样放行,
    /// BossChecklist 的记录 / 召唤 / 掉落内容落在我们的纸面上。<br/>
    /// 反射面缺失、挂钩异常或绘制期抛错 → 停用接管,图鉴走原有 customPortrait 回退路径;
    /// 客户端配置 <see cref="Config.BossLogTakeover"/> 关掉时同样只走回退路径
    /// </summary>
    internal sealed class CEBossLogHook : ICELoader
    {
        private delegate void OrigDraw(object self, SpriteBatch sb);
        private delegate void DrawDetour(OrigDraw orig, object self, SpriteBatch sb);

        /// <summary>钩子在位且未因异常停用</summary>
        public static bool Armed { get; private set; }

        /// <summary>氛围缓入时长(秒)</summary>
        private const float FadeIn = 0.35f;
        /// <summary>氛围缓出时长(秒)</summary>
        private const float FadeOut = 0.3f;
        /// <summary>翻到本模组页后皮的入场演出时长(秒)</summary>
        private const float SettleTime = 0.45f;

        //==================== 帧态 ====================

        /// <summary>当前选中的本模组条目(null = 原版页 / 未开书)</summary>
        private static CEBossLogEntry active;
        /// <summary>氛围层沿用的条目:翻走后淡出期仍保留</summary>
        private static CEBossLogEntry ambient;
        /// <summary>当前页 EntryInfo 反射对象</summary>
        private static object activeEntry;
        private static float blend;
        private static float settle;
        private static float time;
        private static long lastStamp;
        private static Rectangle bookRect;
        private static Rectangle leftRect;
        private static Rectangle rightRect;
        /// <summary>上游页字段缺失:页矩形改由书矩形按版式常量推算(书封钩子里拿到真书矩形后再算)</summary>
        private static bool pagesDerived;

        void ICELoader.SetupData() {
            if (Main.dedServ || !ModLoader.TryGetMod("BossChecklist", out Mod checklist)) {
                return;
            }
            if (!CEBossLogReflect.Resolve(checklist, out string missing)) {
                CalamityEntropy.Instance.Logger.Warn($"CEBossLogHook: BossChecklist 反射面缺失({missing}),整本书接管停用,图鉴走 customPortrait 回退");
                return;
            }
            try {
                Attach(CEBossLogReflect.PanelDraw, new DrawDetour(OnPanelDraw));
                Attach(CEBossLogReflect.LogDraw, new DrawDetour(OnLogDraw));
                Armed = true;
                CalamityEntropy.Instance.Logger.Info("CEBossLogHook: 图鉴整本书接管已就位(LogPanel.Draw / BossLogUI.Draw)");
            } catch (Exception e) {
                Armed = false;
                CalamityEntropy.Instance.Logger.Warn($"CEBossLogHook: 挂钩失败,整本书接管停用: {e.Message}");
            }
        }

        /// <summary>钩子本体由 <see cref="EModHooks.UnLoadData"/> 在模组卸载末尾统一撤销,这里只清状态、先熄火</summary>
        void ICELoader.UnLoadData() {
            Armed = false;
            CEBossLogReflect.Clear();
            CEBossLogRegistry.Clear();
            active = ambient = null;
            activeEntry = null;
            blend = settle = time = 0f;
            lastStamp = 0;
        }

        private static void Attach(MethodInfo method, Delegate detour) {
            if (EModHooks.Add(method, detour) == null) {
                throw new InvalidOperationException($"无法挂钩 {method?.DeclaringType?.Name}.{method?.Name}");
            }
        }

        /// <summary>绘制期出错:记一次日志并停用接管,本帧余下交回 orig,下一帧起图鉴回到回退路径</summary>
        private static void Disarm(Exception e) {
            Armed = false;
            active = ambient = null;
            blend = 0f;
            CalamityEntropy.Instance.Logger.Warn($"CEBossLogHook: 绘制异常,整本书接管停用: {e}");
        }

        //==================== BossLogUI.Draw ====================

        private static void OnLogDraw(OrigDraw orig, object self, SpriteBatch sb) {
            if (!Armed) {
                orig(self, sb);
                return;
            }
            try {
                BeginFrame();
            } catch (Exception e) {
                Disarm(e);
                orig(self, sb);
                return;
            }

            if (ambient != null && blend > 0f) {
                try {
                    CEBossLogSkin.DrawAmbience(sb, ambient.Actor.Theme, bookRect, time, blend);
                } catch (Exception e) {
                    Disarm(e);
                }
            }

            orig(self, sb);

            if (Armed && active != null) {
                try {
                    Rectangle spine = CEBossLogSkin.SpineOf(bookRect, leftRect, rightRect);
                    Rectangle bottom = CEBossLogSkin.BottomMarginOf(bookRect, leftRect, rightRect);
                    active.Actor.Theme.DrawOrnament(sb, bookRect, spine, bottom, time, CEBossLogSkin.Ease(blend));
                } catch (Exception e) {
                    Disarm(e);
                }
            }
        }

        /// <summary>每帧一次:判定当前页是否本模组条目、推时钟与混合量、取书与页矩形</summary>
        private static void BeginFrame() {
            object logUi = CEBossLogReflect.LogUI();
            bool visible = CEBossLogReflect.LogVisible(logUi);
            bool enabled = Config.Instance == null || Config.Instance.BossLogTakeover;

            CEBossLogEntry now = null;
            object entry = null;
            if (enabled && visible && CEBossLogReflect.PageNum(logUi) >= 0) {
                entry = CEBossLogReflect.CurrentEntry(logUi);
                if (CEBossLogRegistry.TryGet(CEBossLogReflect.EntryKey(entry), out CEBossLogEntry found)) {
                    now = found;
                }
            }

            //墙钟:暂停时 UI 照常绘制,动画也照常呼吸;长时间未绘(合书)后回来限幅,不跳变
            long stamp = Stopwatch.GetTimestamp();
            float dt = lastStamp == 0 ? 1f / 60f
                : MathHelper.Clamp((float)((stamp - lastStamp) / (double)Stopwatch.Frequency), 0f, 0.1f);
            lastStamp = stamp;
            time += dt;

            if (now != null) {
                if (now != active) {
                    settle = 0f;
                }
                ambient = now;
                blend = MathF.Min(1f, blend + dt / FadeIn);
                settle = MathF.Min(1f, settle + dt / SettleTime);
            }
            else {
                blend = MathF.Max(0f, blend - dt / FadeOut);
                if (blend <= 0f) {
                    ambient = null;
                }
            }
            active = now;
            activeEntry = now != null ? entry : null;

            if (now == null && ambient == null) {
                return;
            }
            //书与页矩形:优先读上游字段,缺失时按上游版式常量从书矩形推算
            UIElement book = CEBossLogReflect.BookArea(logUi);
            if (book != null) {
                bookRect = book.GetInnerDimensions().ToRectangle();
            }
            UIElement left = CEBossLogReflect.LeftPage(logUi);
            UIElement right = CEBossLogReflect.RightPage(logUi);
            pagesDerived = left == null || right == null;
            if (!pagesDerived) {
                leftRect = left.GetInnerDimensions().ToRectangle();
                rightRect = right.GetInnerDimensions().ToRectangle();
            }
            else {
                leftRect = CEBossLogSkin.LeftPageOf(bookRect);
                rightRect = CEBossLogSkin.RightPageOf(bookRect);
            }
        }

        //==================== LogPanel.Draw ====================

        private static void OnPanelDraw(OrigDraw orig, object self, SpriteBatch sb) {
            if (!Armed || active == null || self is not UIElement panel) {
                orig(self, sb);
                return;
            }
            string id;
            try {
                id = CEBossLogReflect.PanelId(panel);
            } catch (Exception e) {
                Disarm(e);
                orig(self, sb);
                return;
            }

            try {
                switch (id) {
                    case "":
                        DrawBook(panel, sb);
                        return;
                    case "PageOne":
                        DrawPageOne(panel, sb);
                        return;
                    default:
                        orig(self, sb);
                        return;
                }
            } catch (Exception e) {
                Disarm(e);
                orig(self, sb);
            }
        }

        /// <summary>书封:跳过上游的书皮 / 纸面贴图,整本按主题重画(页面内容坐标不动)</summary>
        private static void DrawBook(UIElement panel, SpriteBatch sb) {
            HideMouseOver(panel);
            Rectangle book = panel.GetInnerDimensions().ToRectangle();
            bookRect = book;
            if (pagesDerived) {
                leftRect = CEBossLogSkin.LeftPageOf(book);
                rightRect = CEBossLogSkin.RightPageOf(book);
            }
            CEBossLogSkin.DrawBook(sb, active.Actor.Theme, book, leftRect, rightRect, time, settle);
        }

        /// <summary>
        /// 左页全权重绘。层序修正:场景先铺满 → 子元素(上一页按钮等)回到场景之上 → 标题块。
        /// 上游原顺序是子元素先画、customPortrait 后画,不透明场景会把按钮盖掉
        /// </summary>
        private static void DrawPageOne(UIElement panel, SpriteBatch sb) {
            HideMouseOver(panel);
            CEBossLogEntry entry = active;
            CEBossLogTheme theme = entry.Actor.Theme;
            Rectangle page = panel.GetInnerDimensions().ToRectangle();
            Color mask = CEBossLogReflect.MaskBoss(activeEntry);

            Rectangle canvas = CEBossLogSkin.SceneCanvas(page, theme);
            CEBossPortraitStage.DrawScene(sb, canvas, mask, entry.Actor);
            CEBossLogSkin.DrawSceneFrame(sb, theme, canvas, time, settle);

            CEBossLogReflect.DrawChildren(panel, sb);

            string name = CEBossLogReflect.EntryDisplayName(activeEntry) ?? entry.FallbackName;
            IReadOnlyList<Asset<Texture2D>> heads = CEBossLogReflect.EntryHeads(activeEntry) ?? entry.FallbackHeads();
            bool downed = CEBossLogReflect.EntryDowned(activeEntry) ?? entry.Downed();
            Rectangle hover = CEBossLogSkin.DrawTitleBlock(sb, theme, page, name, CalamityEntropy.Instance.DisplayNameClean,
                heads, downed, mask, settle);

            //头图标悬停:沿用上游的击败 / 未击败文案键,由 BossChecklist 的悬停层绘制
            if (hover != Rectangle.Empty && hover.Contains(Main.MouseScreen.ToPoint())) {
                bool marked = CEBossLogReflect.EntryMarked(activeEntry);
                CEBossLogReflect.SetHoverText(downed ? "Log.EntryPage.Defeated" : "Log.EntryPage.Undefeated",
                    [Main.worldName, marked ? "*" : ""], downed ? Colors.RarityGreen : Colors.RarityRed);
            }
        }

        /// <summary>镜像上游 LogUIElement.Draw 的鼠标遮挡:悬停在书上时屏蔽世界交互与物块悬停提示</summary>
        private static void HideMouseOver(UIElement element) {
            if (!element.ContainsPoint(Main.MouseScreen) || PlayerInput.IgnoreMouseInterface) {
                return;
            }
            Main.LocalPlayer.mouseInterface = true;
            Main.mouseText = true;
            Main.LocalPlayer.cursorItemIconEnabled = false;
            Main.LocalPlayer.cursorItemIconID = -1;
            Main.ItemIconCacheUpdate(0);
        }
    }
}
