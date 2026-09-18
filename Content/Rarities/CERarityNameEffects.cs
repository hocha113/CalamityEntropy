using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>
    /// 稀有度名称特效的绘制原语。全部在调用方当前的 SpriteBatch(提示框 Deferred + AlphaBlend + UIScaleMatrix)里直绘:
    /// 不 End/Begin、不切混合态。加色一律 A=0,预乘贴图下这等价于 Additive,透明底与黑底亮度贴图都只走这一条路。
    /// 所有随机量都走 <see cref="Hash01"/> 确定性哈希,提示框逐帧重画也不会抖。
    /// </summary>
    public static class CERarityNameEffects
    {
        private static readonly Rectangle PixelSrc = new(0, 0, 1, 1);
        private static DynamicSpriteFont Font => FontAssets.MouseText.Value;
        private static Texture2D Pixel => TextureAssets.MagicPixel.Value;

        //水晶字的两张专用贴图(自有 Ports 素材),加载期由 VaultLoaden 赋值,服务端为 null
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/CrystalTextGlow")]
        private static Asset<Texture2D> crystalGlowTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/CrystalTextSparkle")]
        private static Asset<Texture2D> crystalSparkleTex;

        /// <summary>客户端总开关,关掉后本模组稀有度的名字交回原版画普通描边字</summary>
        public static bool Enabled => Config.Instance?.RarityNameEffects ?? true;

        /// <summary>
        /// EGlobalItem 名称行的唯一分发入口。命中本模组稀有度且开关开着就自绘并返回 true;
        /// 原版/他模稀有度、或开关关着,返回 false 交回原版绘制
        /// </summary>
        public static bool TryDrawItemName(Item item, DrawableTooltipLine line) {
            if (item == null || line == null || !Enabled) {
                return false;
            }
            if (RarityLoader.GetRarity(item.rare) is not CERarity rarity) {
                return false;
            }
            Color color = line.OverrideColor ?? line.Color;
            rarity.DrawName(Main.spriteBatch, item, line.Text, new Vector2(line.X, line.Y), color, line.BaseScale, Main.GlobalTimeWrappedHourly);
            return true;
        }

        #region 文本
        /// <summary>原版口径:四向 2px 黑阴影 + 正文,解析聊天标签</summary>
        public static void DrawPlain(SpriteBatch sb, string text, Vector2 pos, Color color, Vector2 scale) {
            ChatManager.DrawColorCodedStringWithShadow(sb, Font, text, pos, color, 0f, Vector2.Zero, scale);
        }

        /// <summary>裸字:不解析标签、不带阴影。字号由原版提示框行给定</summary>
        public static void DrawText(SpriteBatch sb, string text, Vector2 pos, Color color, Vector2 scale) {
            sb.DrawString(Font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>四向阴影,不含正文</summary>
        public static void DrawShadow(SpriteBatch sb, string text, Vector2 pos, Color color, Vector2 scale, float offset = 2f) {
            foreach (Vector2 dir in ChatManager.ShadowDirections) {
                DrawText(sb, text, pos + dir * offset, color, scale);
            }
        }

        /// <summary>环形描边,不含正文</summary>
        public static void DrawOutline(SpriteBatch sb, string text, Vector2 pos, Color color, Vector2 scale, float radius, int directions = 8) {
            for (int i = 0; i < directions; i++) {
                Vector2 offset = new Vector2(radius, 0f).RotatedBy(MathHelper.TwoPi * i / directions);
                DrawText(sb, text, pos + offset, color, scale);
            }
        }
        #endregion

        #region 逐字布局
        /// <summary>
        /// 逐字位置表,沿用整段前缀测量保留字距;单例复用不每帧分配。
        /// 装饰元素(底光、粒子)的纵向落点用 <see cref="BandY"/>,它已含多语言字体的纵向修正
        /// </summary>
        public sealed class GlyphLayout
        {
            public int Count;
            public string[] Glyphs = new string[32];
            public Vector2[] Positions = new Vector2[32];
            public float[] Widths = new float[32];
            /// <summary>整行宽</summary>
            public float Width;
            /// <summary>行高(字体行距 × 缩放)</summary>
            public float Height;
            public Vector2 Origin;
            /// <summary>非中文字体字身偏下的经验修正,沿用旧实现的 4px</summary>
            public float CultureY;

            /// <summary>第 i 字中心 X</summary>
            public float CenterX(int i) => Positions[i].X + Widths[i] * 0.5f;

            /// <summary>整行中心 X</summary>
            public float MidX => Origin.X + Width * 0.5f;

            /// <summary>字身可见带内的 Y,t = 0 顶、t = 1 底</summary>
            public float BandY(float t) => Origin.Y + CultureY + Height * (0.15f + 0.55f * t);

            /// <summary>字身视觉中心 Y</summary>
            public float MidY => BandY(0.5f);

            internal void Ensure(int n) {
                if (Glyphs.Length >= n) {
                    return;
                }
                int size = Math.Max(n, Glyphs.Length * 2);
                Array.Resize(ref Glyphs, size);
                Array.Resize(ref Positions, size);
                Array.Resize(ref Widths, size);
            }
        }

        private static readonly GlyphLayout sharedLayout = new();
        private static readonly Dictionary<char, string> glyphStrings = [];

        private static string GlyphString(char c) {
            if (!glyphStrings.TryGetValue(c, out string s)) {
                s = c.ToString();
                glyphStrings[c] = s;
            }
            return s;
        }

        /// <summary>多语言纵向修正:中文字体字身贴顶,其余语言经验值下移 4px(沿用旧 tooltipNameUpList 规则)</summary>
        public static float CultureYOffset => CELists.tooltipNameUpList.Contains(Language.ActiveCulture.Name) ? 0f : 4f;

        public static GlyphLayout Layout(string text, Vector2 pos, Vector2 scale) {
            GlyphLayout layout = sharedLayout;
            int n = text.Length;
            layout.Ensure(n);
            layout.Count = n;
            layout.Origin = pos;
            layout.Height = Font.MeasureString(" ").Y * scale.Y;
            layout.CultureY = CultureYOffset;
            float prefix = 0f;
            for (int i = 0; i < n; i++) {
                layout.Glyphs[i] = GlyphString(text[i]);
                layout.Positions[i] = new Vector2(pos.X + prefix, pos.Y);
                float next = Font.MeasureString(text[..(i + 1)]).X * scale.X;
                layout.Widths[i] = MathF.Max(0f, next - prefix);
                prefix = next;
            }
            layout.Width = prefix;
            return layout;
        }

        public static void DrawGlyph(SpriteBatch sb, GlyphLayout layout, int i, Vector2 offset, Color color, Vector2 scale) {
            sb.DrawString(Font, layout.Glyphs[i], layout.Positions[i] + offset, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>整行逐字按 X 渐变着色,t 由各字中心相对行宽的位置给出</summary>
        public static void DrawGradient(SpriteBatch sb, GlyphLayout layout, Color left, Color right, Vector2 scale, Vector2 offset = default) {
            for (int i = 0; i < layout.Count; i++) {
                float t = layout.Width > 0f ? (layout.CenterX(i) - layout.Origin.X) / layout.Width : 0f;
                DrawGlyph(sb, layout, i, offset, Color.Lerp(left, right, t), scale);
            }
        }
        #endregion

        #region 数值
        /// <summary>确定性哈希 [0,1)</summary>
        public static float Hash01(int a, int b = 0) {
            uint h = (uint)(a * 374761393) ^ (uint)(b * 668265263) ^ 0x9E3779B9u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>周期呼吸 [min,max]</summary>
        public static float Breath(float time, float period, float min, float max) {
            return MathHelper.Lerp(min, max, 0.5f + 0.5f * MathF.Sin(time * MathHelper.TwoPi / period));
        }

        /// <summary>只缩放 RGB,保留 A(mouseTextColor 衰减已在 A 里)</summary>
        public static Color Scale(Color color, float k) {
            return new Color(
                (int)MathHelper.Clamp(color.R * k, 0f, 255f),
                (int)MathHelper.Clamp(color.G * k, 0f, 255f),
                (int)MathHelper.Clamp(color.B * k, 0f, 255f),
                color.A);
        }

        /// <summary>调色板色按行衰减压暗,同步 mouseTextColor 呼吸</summary>
        public static Color Fade(Color palette, float fade) => palette * fade;

        /// <summary>行衰减系数,取自 tML 传入的名称行颜色</summary>
        public static float FadeOf(Color lineColor) => lineColor.A / 255f;

        /// <summary>加色形态:A 清零,在 AlphaBlend 批次里等价于 Additive</summary>
        public static Color Additive(Color color) => color with { A = 0 };

        /// <summary>周期性一次性事件:返回当前周期序号与周期内进度 [0,1)</summary>
        public static float Cycle(float time, float period, float phase, out int index) {
            float cycle = time / period + phase;
            index = (int)MathF.Floor(cycle);
            return cycle - index;
        }
        #endregion

        #region 贴图
        /// <summary>柔光点(Glow 贴图,恒 A=0 加色),size 为直径像素</summary>
        public static void DrawMote(SpriteBatch sb, Vector2 center, float size, Color color) {
            Texture2D tex = CEExtraAssets.Glow;
            if (tex == null) {
                return;
            }
            sb.Draw(tex, center, null, Additive(color), 0f, tex.Size() * 0.5f, size / tex.Width, SpriteEffects.None, 0f);
        }

        /// <summary>四芒星光点(StarTexture_White,恒 A=0 加色),size 为直径像素</summary>
        public static void DrawStar(SpriteBatch sb, Vector2 center, float size, Color color, float rotation = 0f) {
            Texture2D tex = CEExtraAssets.StarTexture_White;
            if (tex == null) {
                return;
            }
            sb.Draw(tex, center, null, Additive(color), rotation, tex.Size() * 0.5f, size / tex.Width, SpriteEffects.None, 0f);
        }

        /// <summary>细光丝(Ray 贴图横向拉长,恒 A=0 加色)</summary>
        public static void DrawStreak(SpriteBatch sb, Vector2 center, float length, float thickness, Color color, float rotation = 0f) {
            Texture2D tex = CEExtraAssets.Ray;
            if (tex == null) {
                return;
            }
            sb.Draw(tex, center, null, Additive(color), rotation, tex.Size() * 0.5f, new Vector2(length / tex.Width, thickness / tex.Height), SpriteEffects.None, 0f);
        }

        /// <summary>烟团(Smoke 贴图,恒 A=0 加色),size 为直径像素</summary>
        public static void DrawSmoke(SpriteBatch sb, Vector2 center, float size, Color color, float rotation) {
            Texture2D tex = CEExtraAssets.Smoke;
            if (tex == null) {
                return;
            }
            sb.Draw(tex, center, null, Additive(color), rotation, tex.Size() * 0.5f, size / tex.Width, SpriteEffects.None, 0f);
        }

        /// <summary>铺在整行字后面的横向柔光带:宽 = 行宽 + widthPad,高 = 行高 × heightScale</summary>
        public static void DrawGlowBand(SpriteBatch sb, GlyphLayout layout, Color color, float widthPad, float heightScale) {
            Texture2D tex = CEExtraAssets.Glow;
            if (tex == null) {
                return;
            }
            Vector2 center = new(layout.MidX, layout.MidY);
            Vector2 scale = new((layout.Width + widthPad) / tex.Width, layout.Height * heightScale / tex.Height);
            sb.Draw(tex, center, null, Additive(color), 0f, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>实心小方屑(真 alpha 像素)</summary>
        public static void DrawFleck(SpriteBatch sb, Vector2 center, float size, Color color) {
            sb.Draw(Pixel, center, PixelSrc, color, 0f, new Vector2(0.5f), size, SpriteEffects.None, 0f);
        }

        /// <summary>水平细线,start 为左端中点</summary>
        public static void DrawHLine(SpriteBatch sb, Vector2 start, float length, float thickness, Color color) {
            sb.Draw(Pixel, start, PixelSrc, color, 0f, new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        /// <summary>竖直细线,start 为上端中点</summary>
        public static void DrawVLine(SpriteBatch sb, Vector2 start, float length, float thickness, Color color) {
            sb.Draw(Pixel, start, PixelSrc, color, 0f, new Vector2(0.5f, 0f), new Vector2(thickness, length), SpriteEffects.None, 0f);
        }
        #endregion

        #region 水晶字
        //水晶字:脉动旋转的多向淡影 + 竖向光条 + 逐字渐变亮边裹黑芯 + 上浮闪粒。
        //ShiningViolet 的本体,也供个别物品换色复用(FlowingLight 的鎏金描字、FadingRoseateReverie 的粉字)
        private const float CrystalSparkleRise = 4.5f;

        /// <summary>提示框行重载:<paramref name="textColor"/> 起始色,<paramref name="gradientEnd"/> 渐变终止色,<paramref name="sparkleColor"/> 闪粒加色</summary>
        public static void DrawCrystal(Item item, DrawableTooltipLine line, Color textColor, Color gradientEnd, Color sparkleColor, bool sparkles = true) {
            DrawCrystal(Main.spriteBatch, item, line.Text, new Vector2(line.X, line.Y), line.BaseScale, Main.GlobalTimeWrappedHourly,
                textColor, gradientEnd, textColor * 0.6f, sparkleColor, sparkles);
        }

        public static void DrawCrystal(SpriteBatch sb, Item item, string text, Vector2 pos, Vector2 scale, float time,
            Color textColor, Color gradientEnd, Color lightColor, Color sparkleColor, bool sparkles) {
            Texture2D glow = crystalGlowTex?.Value;
            Texture2D sparkle = crystalSparkleTex?.Value;
            GlyphLayout layout = Layout(text, pos, scale);
            Vector2 size = new(layout.Width, layout.Height);

            //脉动旋转的多向淡影
            float pulsing = 2.5f + MathF.Sin(time * 5f);
            for (float f = 0f; f < MathHelper.TwoPi; f += 0.79f) {
                DrawText(sb, text, pos + new Vector2(pulsing, 0f).RotatedBy(f + time * 2f % MathHelper.TwoPi), textColor * 0.5f, scale);
            }

            //竖向光条横过整行(贴图本身是竖条,转 90° 后沿行宽拉伸)
            if (glow != null) {
                Vector2 glowPosition = new(layout.MidX, pos.Y + size.Y / 3f);
                sb.Draw(glow, glowPosition, null, lightColor, MathHelper.PiOver2, new Vector2(6f, 33f),
                    new Vector2(1.6f * scale.Y, size.X / glow.Height * 1.2f), SpriteEffects.None, 0f);
            }

            //逐字渐变亮边(2px 四向)裹黑芯
            for (int i = 0; i < layout.Count; i++) {
                float t = layout.Count > 1 ? i / (layout.Count - 1f) : 0f;
                Color edge = Color.Lerp(textColor, gradientEnd, t) * 2f;
                foreach (Vector2 dir in ChatManager.ShadowDirections) {
                    DrawGlyph(sb, layout, i, dir * 2f, edge, scale);
                }
            }
            for (int i = 0; i < layout.Count; i++) {
                DrawGlyph(sb, layout, i, Vector2.Zero, Color.Black, scale);
            }

            if (!sparkles || sparkle == null) {
                return;
            }

            //上浮闪粒:落点、相位、转速全部确定性哈希,种子取行宽与物品类型,不随帧抖
            int seed = (int)size.X * 31 + (item?.type ?? 0);
            int sparkleCount = (int)(size.X / 6f) + 1;
            Color additiveSparkle = Additive(sparkleColor);
            Vector2 sparkleOrigin = sparkle.Size() * 0.5f;
            for (int i = 0; i < sparkleCount; i++) {
                Vector2 v = new(Hash01(seed, i * 4 + 1) * size.X, Hash01(seed, i * 4 + 2) * size.Y * 0.6f + 1f);
                float life = (time * 4f + Hash01(seed, i * 4 + 3) * MathHelper.TwoPi) % MathHelper.TwoPi;
                float sinValue = MathF.Sin(life);
                Color white = new Color(200 + lightColor.R / 20, 200 + lightColor.G / 20, 200 + lightColor.B / 20, 255) * sinValue;
                float rotation = time * (0.8f + 0.7f * Hash01(seed, i * 4 + 4));
                Vector2 p = new Vector2(pos.X, pos.Y - life * CrystalSparkleRise + 2f) + v;
                float progress = life / MathHelper.TwoPi;

                sb.Draw(sparkle, p + new Vector2(0f, 1f), null, white, rotation, sparkleOrigin, progress * 0.3f, SpriteEffects.None, 0f);
                sb.Draw(sparkle, p, null, white * 0.5f, rotation, sparkleOrigin, progress, SpriteEffects.None, 0f);

                float scale2 = (MathF.Sin(life / MathHelper.PiOver2) + 1f) * 0.2f;
                float scale3 = progress * 0.15f;
                sb.Draw(sparkle, p, null, additiveSparkle * sinValue, rotation, sparkleOrigin, new Vector2(scale3, scale3) * 1.5f, SpriteEffects.None, 0f);
                sb.Draw(sparkle, p, null, additiveSparkle * sinValue, rotation, sparkleOrigin, new Vector2(scale2, scale3), SpriteEffects.None, 0f);
                sb.Draw(sparkle, p, null, additiveSparkle * sinValue, rotation, sparkleOrigin, new Vector2(scale3, scale2), SpriteEffects.None, 0f);
            }
        }
        #endregion
    }
}
