using CalamityEntropy.Assets.Register;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace CalamityEntropy.Core.Integrations.BossLog
{
    /// <summary>
    /// 图鉴文字门面:标题 / 标签两档字号,行高跟随当前语言字体(zh-Hans 界面字体行高 29px 对 en-US 22px),
    /// 标题超宽时按比例缩小而不截断
    /// </summary>
    internal static class CEBossLogText
    {
        public const float TitleScale = 1.15f;
        public const float LabelScale = 0.82f;
        /// <summary>标题最多缩到基准的几成</summary>
        private const float MinFit = 0.55f;

        public static DynamicSpriteFont Font => FontAssets.MouseText?.Value;

        public static float TitleLineHeight => LineHeight(TitleScale);
        public static float LabelLineHeight => LineHeight(LabelScale);

        public static float LineHeight(float scale) {
            DynamicSpriteFont font = Font;
            return (font == null ? 22f : font.LineSpacing) * scale;
        }

        /// <summary>在 <paramref name="maxWidth"/> 内放下 <paramref name="text"/> 所需的缩放(不放大)</summary>
        public static float Fit(string text, float baseScale, float maxWidth) {
            DynamicSpriteFont font = Font;
            if (font == null || string.IsNullOrEmpty(text) || maxWidth <= 0f) {
                return baseScale;
            }
            float w = font.MeasureString(text).X;
            if (w <= 0f || w * baseScale <= maxWidth) {
                return baseScale;
            }
            return MathF.Max(baseScale * MinFit, maxWidth / w);
        }

        /// <summary>左上角锚定的带影文字</summary>
        public static void Draw(SpriteBatch sb, string text, Vector2 topLeft, float scale, Color color) {
            DynamicSpriteFont font = Font;
            if (font == null || string.IsNullOrEmpty(text)) {
                return;
            }
            Color shadow = Color.Black * (color.A / 255f) * 0.8f;
            ChatManager.DrawColorCodedStringWithShadow(sb, font, text, topLeft, color, shadow, 0f, Vector2.Zero, new Vector2(scale), -1f, 1.6f);
        }
    }

    /// <summary>
    /// 图鉴整本书的共享骨架渲染器:书封 / 书脊 / 双页纸面、左页场景框与标题块。
    /// 纯 CPU 层(magic-pixel 矢量 + 少量既有贴图),颜色全部来自 <see cref="CEBossLogTheme"/>;
    /// 所有矩形都在 UI 空间(调用方批次已带 <c>Main.UIScaleMatrix</c>)。
    /// <b>绘制一律不越出书矩形</b>:图鉴是 BossChecklist 的界面,我们只换这本书的皮,
    /// 不在书外铺整屏背景(2026-09-19 用户裁定:整屏氛围与其它模组条目风格割裂)
    /// </summary>
    internal static class CEBossLogSkin
    {
        //上游 BossLogUI 的版式常量(OnInitialize / ResetUIPositioning):反射拿不到页矩形时按此从书矩形推算
        public const int PageW = 375;
        public const int PageH = 480;
        public const int LeftPageDx = 20;
        public const int PageDy = 12;
        public const int RightPageDxFromRight = 15;

        /// <summary>场景画布距页缘</summary>
        public const int SceneInset = 8;
        /// <summary>纸面比页内容矩形外扩多少</summary>
        private const int PaperPad = 4;
        /// <summary>封角切角</summary>
        private const int Chamfer = 8;

        private static readonly Rectangle PixelSrc = new(0, 0, 1, 1);

        internal static Texture2D Pixel => VaultAsset.placeholder2?.Value;

        public static Rectangle LeftPageOf(Rectangle book)
            => new(book.X + LeftPageDx, book.Y + PageDy, PageW, PageH);

        public static Rectangle RightPageOf(Rectangle book)
            => new(book.Right - RightPageDxFromRight - PageW, book.Y + PageDy, PageW, PageH);

        /// <summary>两页之间的书脊带(页矩形贴合或重叠时退回书中线 10px)</summary>
        public static Rectangle SpineOf(Rectangle book, Rectangle left, Rectangle right) {
            int x0 = left.Right + PaperPad;
            int x1 = right.X - PaperPad;
            if (x1 - x0 < 4) {
                int cx = book.Center.X;
                return new Rectangle(cx - 5, book.Y + 2, 10, book.Height - 4);
            }
            return new Rectangle(x0, book.Y + 2, x1 - x0, book.Height - 4);
        }

        /// <summary>底封边(页纸下缘到书底之间):饰件唯一允许的水平落点</summary>
        public static Rectangle BottomMarginOf(Rectangle book, Rectangle left, Rectangle right) {
            int top = Math.Max(left.Bottom, right.Bottom) + PaperPad + 1;
            return new Rectangle(book.X + Chamfer, top, book.Width - Chamfer * 2, Math.Max(0, book.Bottom - 2 - top));
        }

        /// <summary>
        /// 标题带实际高度:主题声明值与字号阶梯(题 + 模组名 + 上下留白)取大。
        /// 中文字体包行高更高,固定值会让模组名压进场景,故随字号撑开
        /// </summary>
        public static int TitleBandOf(CEBossLogTheme theme)
            => Math.Max(theme.TitleBand, (int)MathF.Ceiling(CEBossLogText.TitleLineHeight + CEBossLogText.LabelLineHeight) + 14);

        public static Rectangle SceneCanvas(Rectangle page, CEBossLogTheme theme) {
            int band = TitleBandOf(theme);
            return new Rectangle(page.X + SceneInset, page.Y + band, page.Width - SceneInset * 2, page.Height - band - SceneInset);
        }

        /// <summary>缓入缓出(smoothstep)</summary>
        public static float Ease(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        //==================== 书本体 ====================

        /// <summary>书封 / 书脊 / 双页纸面。<paramref name="settle"/> 为翻到本页后的入场进度 0..1</summary>
        public static void DrawBook(SpriteBatch sb, CEBossLogTheme theme, Rectangle book, Rectangle left, Rectangle right,
            float time, float settle) {
            Texture2D px = Pixel;
            if (px == null) {
                return;
            }
            float s = Ease(settle);

            //紧贴投影:右下偏移一层实底,不做大面积羽化
            FillChamfered(sb, new Rectangle(book.X + 3, book.Y + 5, book.Width, book.Height), Chamfer, Color.Black * 0.38f);

            //封面主体 + 皮革斑驳(Perlin 灰度按 A=0 低量叠色)
            FillChamfered(sb, book, Chamfer, theme.Cover);
            Texture2D noise = CEExtraAssets.Perlin;
            if (noise != null) {
                //内缩一个切角量,别让矩形斑驳漏到切掉的四角外
                sb.Draw(noise, Inflate(book, -Chamfer), new Rectangle(0, 0, noise.Width, noise.Height),
                    theme.CoverEdge with { A = 0 } * 0.07f);
            }

            //封边:外亮线呼吸 + 内退 3px 的暗一档细线
            float breath = 0.55f + 0.1f * MathF.Sin(time * 1.3f);
            OutlineChamfered(sb, book, Chamfer, theme.CoverEdge * breath);
            Stroke(sb, Inflate(book, -3), theme.CoverEdge * 0.22f, 1);

            //书脊:主体 + 两侧高光 + 两端三段压暗
            Rectangle spine = SpineOf(book, left, right);
            Fill(sb, spine, theme.Spine);
            Fill(sb, new Rectangle(spine.X, spine.Y, 1, spine.Height), theme.CoverEdge * 0.5f);
            Fill(sb, new Rectangle(spine.Right - 1, spine.Y, 1, spine.Height), theme.CoverEdge * 0.5f);
            for (int i = 0; i < 3; i++) {
                Color d = Color.Black * (0.28f - i * 0.08f);
                int h = 10 + i * 12;
                Fill(sb, new Rectangle(spine.X, spine.Y, spine.Width, h), d);
                Fill(sb, new Rectangle(spine.X, spine.Bottom - h, spine.Width, h), d);
            }

            DrawPaper(sb, theme, left, outerLeft: true, noise, s, 0);
            DrawPaper(sb, theme, right, outerLeft: false, noise, s, 1);

            //入场光扫:一道细亮带自左向右掠过封面一次,扫完即熄
            if (s < 1f) {
                float x = MathHelper.Lerp(book.X - 40f, book.Right + 40f, s);
                Color sweep = theme.CoverEdge with { A = 0 } * (0.35f * (1f - s));
                sb.Draw(px, new Vector2(x, book.Y), PixelSrc, sweep, 0f, new Vector2(0.5f, 0f),
                    new Vector2(46f, book.Height), SpriteEffects.None, 0f);
            }
        }

        private static void DrawPaper(SpriteBatch sb, CEBossLogTheme theme, Rectangle page, bool outerLeft,
            Texture2D noise, float s, int seed) {
            Rectangle paper = Inflate(page, PaperPad);

            //页缘叠层:外侧与下缘各三条渐淡细线,读作纸叠厚度
            for (int i = 1; i <= 3; i++) {
                Color c = theme.PaperLight * (0.34f - i * 0.09f);
                int off = i * 2;
                int vx = outerLeft ? paper.X - off : paper.Right - 1 + off;
                Fill(sb, new Rectangle(vx, paper.Y + off, 1, paper.Height), c);
                Fill(sb, new Rectangle(paper.X + (outerLeft ? -off : off), paper.Bottom - 1 + off, paper.Width, 1), c);
            }

            Fill(sb, paper, theme.Paper);
            if (noise != null) {
                //纸纤维:两页取噪声图不同区域,别长得一样
                int sx = Math.Min(seed * 128, Math.Max(0, noise.Width - 64));
                int sy = Math.Min(96, Math.Max(0, noise.Height - 64));
                Rectangle src = new(sx, sy, Math.Max(16, noise.Width - sx - 32), Math.Max(16, noise.Height - sy - 32));
                sb.Draw(noise, paper, src, theme.PaperLight with { A = 0 } * (0.075f * s));
            }
            Stroke(sb, paper, theme.Rule * 0.55f, 1);
            Stroke(sb, Inflate(paper, -3), theme.Rule * 0.2f, 1);
        }

        //==================== 左页:场景框与标题块 ====================

        /// <summary>场景画布外框:细线 + 四角亮角(随入场展开)+ 上缘接缝光</summary>
        public static void DrawSceneFrame(SpriteBatch sb, CEBossLogTheme theme, Rectangle canvas, float time, float settle) {
            if (Pixel == null) {
                return;
            }
            float s = Ease(settle);
            Rectangle frame = Inflate(canvas, 1);
            Stroke(sb, frame, theme.Accent * 0.4f, 1);

            int len = (int)(14f * s);
            if (len >= 1) {
                Color tick = theme.Accent * (0.85f + 0.15f * MathF.Sin(time * 2.1f));
                CornerTick(sb, frame.X, frame.Y, 1, 1, len, tick);
                CornerTick(sb, frame.Right - 1, frame.Y, -1, 1, len, tick);
                CornerTick(sb, frame.X, frame.Bottom - 1, 1, -1, len, tick);
                CornerTick(sb, frame.Right - 1, frame.Bottom - 1, -1, -1, len, tick);
            }

            for (int i = 0; i < 6; i++) {
                Color c = theme.Accent with { A = 0 } * (0.16f * (1f - i / 6f) * s);
                Fill(sb, new Rectangle(canvas.X, canvas.Y + i, canvas.Width, 1), c);
            }
        }

        /// <summary>
        /// 左页标题块:Boss 名 / 模组名 / 头图标 / 击败标记。
        /// <paramref name="mask"/> 沿用 BossChecklist 的进度蒙版(黑=剪影头像)。返回头图标悬停区(UI 空间)
        /// </summary>
        public static Rectangle DrawTitleBlock(SpriteBatch sb, CEBossLogTheme theme, Rectangle page, string name, string modName,
            IReadOnlyList<Asset<Texture2D>> heads, bool downed, Color mask, float settle) {
            if (Pixel == null) {
                return Rectangle.Empty;
            }
            float s = Ease(settle);
            float rise = (1f - s) * 6f;

            //头图标:贴右缘向左排;上游按倒序画,列表首张落在最右
            int x = page.Right - 10;
            int top = page.Y + 8;
            Rectangle hover = Rectangle.Empty;
            Rectangle rightmost = Rectangle.Empty;
            if (heads != null) {
                for (int i = heads.Count - 1; i >= 0; i--) {
                    Texture2D head = heads[i]?.Value;
                    if (head == null) {
                        continue;
                    }
                    Rectangle dst = new(x - head.Width, top, head.Width, head.Height);
                    sb.Draw(head, dst, mask * s);
                    hover = hover == Rectangle.Empty ? dst : Rectangle.Union(hover, dst);
                    if (rightmost == Rectangle.Empty) {
                        rightmost = dst;
                    }
                    x -= head.Width + 2;
                }
            }
            if (rightmost != Rectangle.Empty) {
                DrawMark(sb, new Vector2(rightmost.Center.X, rightmost.Bottom + 2), downed, s);
            }

            //名与模组名:左起,宽度让出头图标
            int headsLeft = hover == Rectangle.Empty ? page.Right - 10 : hover.X;
            float availW = headsLeft - 8 - (page.X + 12);
            float titleScale = CEBossLogText.Fit(name, CEBossLogText.TitleScale, availW);
            float titleLine = CEBossLogText.LineHeight(titleScale);
            Vector2 namePos = new(page.X + 12, page.Y + 6 + rise);
            CEBossLogText.Draw(sb, name, namePos, titleScale, theme.Accent * s);
            CEBossLogText.Draw(sb, modName, new Vector2(page.X + 13, namePos.Y + titleLine + 1f),
                CEBossLogText.LabelScale, theme.Muted * s);

            //标题带底线:随入场自左向右画开
            int lineW = (int)(availW * s);
            if (lineW > 0) {
                Fill(sb, new Rectangle(page.X + 12, page.Y + TitleBandOf(theme) - 5, lineW, 1), theme.Rule * 0.7f);
            }
            return hover;
        }

        /// <summary>击败 ✓ / 未击败 ✗,两笔矢量</summary>
        private static void DrawMark(SpriteBatch sb, Vector2 c, bool downed, float alpha) {
            if (downed) {
                Color g = new Color(120, 224, 130) * alpha;
                Line(sb, c + new Vector2(-6f, 0f), c + new Vector2(-2f, 4f), 2f, g);
                Line(sb, c + new Vector2(-2f, 4f), c + new Vector2(6f, -5f), 2f, g);
            }
            else {
                Color r = new Color(226, 84, 84) * alpha;
                Line(sb, c + new Vector2(-5f, -5f), c + new Vector2(5f, 5f), 2f, r);
                Line(sb, c + new Vector2(5f, -5f), c + new Vector2(-5f, 5f), 2f, r);
            }
        }

        //==================== 图元 ====================

        internal static void Fill(SpriteBatch sb, Rectangle r, Color c) {
            if (r.Width <= 0 || r.Height <= 0) {
                return;
            }
            sb.Draw(Pixel, r, PixelSrc, c);
        }

        internal static void Stroke(SpriteBatch sb, Rectangle r, Color c, int t) {
            Fill(sb, new Rectangle(r.X, r.Y, r.Width, t), c);
            Fill(sb, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            Fill(sb, new Rectangle(r.X, r.Y + t, t, r.Height - 2 * t), c);
            Fill(sb, new Rectangle(r.Right - t, r.Y + t, t, r.Height - 2 * t), c);
        }

        internal static void Line(SpriteBatch sb, Vector2 a, Vector2 b, float thick, Color c) {
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.5f) {
                return;
            }
            sb.Draw(Pixel, a, PixelSrc, c, d.ToRotation(), new Vector2(0f, 0.5f),
                new Vector2(len, thick), SpriteEffects.None, 0f);
        }

        internal static Rectangle Inflate(Rectangle r, int d) => new(r.X - d, r.Y - d, r.Width + 2 * d, r.Height + 2 * d);

        /// <summary>切角矩形:十字主体 + 四角台阶小块,各块互不重叠,半透明色也不会叠深</summary>
        private static void FillChamfered(SpriteBatch sb, Rectangle r, int cut, Color c) {
            Fill(sb, new Rectangle(r.X + cut, r.Y, r.Width - cut * 2, r.Height), c);
            Fill(sb, new Rectangle(r.X, r.Y + cut, cut, r.Height - cut * 2), c);
            Fill(sb, new Rectangle(r.Right - cut, r.Y + cut, cut, r.Height - cut * 2), c);
            int step = cut / 3 + 1;
            int side = cut - step;
            Fill(sb, new Rectangle(r.X + step, r.Y + step, side, side), c);
            Fill(sb, new Rectangle(r.Right - cut, r.Y + step, side, side), c);
            Fill(sb, new Rectangle(r.X + step, r.Bottom - cut, side, side), c);
            Fill(sb, new Rectangle(r.Right - cut, r.Bottom - cut, side, side), c);
        }

        private static void OutlineChamfered(SpriteBatch sb, Rectangle r, int cut, Color c) {
            Fill(sb, new Rectangle(r.X + cut, r.Y, r.Width - cut * 2, 1), c);
            Fill(sb, new Rectangle(r.X + cut, r.Bottom - 1, r.Width - cut * 2, 1), c);
            Fill(sb, new Rectangle(r.X, r.Y + cut, 1, r.Height - cut * 2), c);
            Fill(sb, new Rectangle(r.Right - 1, r.Y + cut, 1, r.Height - cut * 2), c);
            Line(sb, new Vector2(r.X + cut, r.Y + 0.5f), new Vector2(r.X + 0.5f, r.Y + cut), 1f, c);
            Line(sb, new Vector2(r.Right - cut, r.Y + 0.5f), new Vector2(r.Right - 0.5f, r.Y + cut), 1f, c);
            Line(sb, new Vector2(r.X + 0.5f, r.Bottom - cut), new Vector2(r.X + cut, r.Bottom - 0.5f), 1f, c);
            Line(sb, new Vector2(r.Right - 0.5f, r.Bottom - cut), new Vector2(r.Right - cut, r.Bottom - 0.5f), 1f, c);
        }

        /// <summary>L 形角标:从角点沿 dx / dy 方向各伸 len,笔宽 2</summary>
        private static void CornerTick(SpriteBatch sb, int cx, int cy, int dx, int dy, int len, Color c) {
            int hx = dx > 0 ? cx : cx - len + 1;
            int hy = dy > 0 ? cy : cy - 1;
            int vx = dx > 0 ? cx : cx - 1;
            int vy = dy > 0 ? cy : cy - len + 1;
            Fill(sb, new Rectangle(hx, hy, len, 2), c);
            Fill(sb, new Rectangle(vx, vy, 2, len), c);
        }
    }
}
