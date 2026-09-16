using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Cooldowns
{
    /// <summary>
    /// 自研冷却行为基类,API 形状对齐原灾厄 CooldownHandler,便于内容类零成本迁移。
    /// 子类以 tML ModType 自动加载,由 <see cref="CECooldownRegistry"/> 按静态 ID 属性建表。
    /// </summary>
    public abstract class CECooldownHandler : ModType
    {
        /// <summary>冷却的字符串 ID。子类用 static new 覆盖;为 null 时注册表回退到类型全名。</summary>
        public static string ID => null;

        /// <summary>该处理器绑定的运行时实例,由 <see cref="CECooldownInstance"/> 创建时赋值。</summary>
        public CECooldownInstance instance;

        protected sealed override void Register() {
            ModTypeLookup<CECooldownHandler>.Register(this);
        }

        #region 玩法行为
        /// <summary>冷却存在期间每帧调用,无论计时是否递减。</summary>
        public virtual void Tick() { }

        /// <summary>冷却自然走完时调用;玩家死亡被清除时不调用。</summary>
        public virtual void OnCompleted() { }

        /// <summary>本帧是否允许计时递减。</summary>
        public virtual bool CanTickDown => true;

        /// <summary>为 true 时玩家死亡不清除该冷却。</summary>
        public virtual bool PersistsThroughDeath => false;

        /// <summary>为 true 时随玩家存档持久化。</summary>
        public virtual bool SavedWithPlayer => true;

        /// <summary>冷却结束音效,null 表示无声。</summary>
        public virtual SoundStyle? EndSound => null;

        /// <summary>是否播放结束音效。</summary>
        public virtual bool ShouldPlayEndSound => true;
        #endregion

        #region 显示
        /// <summary>悬停冷却图标时显示的名称。</summary>
        public virtual LocalizedText DisplayName => LocalizedText.Empty;

        /// <summary>是否出现在冷却栏。</summary>
        public virtual bool ShouldDisplay => true;

        /// <summary>图标贴图路径(20x20 像素)。</summary>
        public virtual string Texture => "";

        /// <summary>紧凑模式叠加进度贴图路径,缺失时回退为图标自裁切。</summary>
        public virtual string OverlayTexture => $"{Texture}Overlay";

        /// <summary>图标外圈描边贴图路径。</summary>
        public virtual string OutlineTexture => $"{Texture}Outline";

        /// <summary>描边与紧凑模式进度叠加的颜色。</summary>
        public virtual Color OutlineColor => Color.White;

        /// <summary>展开模式进度环起始色。</summary>
        public virtual Color CooldownStartColor => Color.Gray;

        /// <summary>展开模式进度环结束色。</summary>
        public virtual Color CooldownEndColor => Color.White;

        /// <summary>展开模式绘制:进度环 + 描边 + 图标。进度环为纯 SpriteBatch 分段圆弧,不依赖着色器。</summary>
        public virtual void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale) {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;

            DrawProgressRing(spriteBatch, position, opacity, scale);
            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>紧凑模式绘制:描边 + 图标 + 自上而下裁切的进度叠加。</summary>
        public virtual void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale) {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;

            // 部分冷却没有专门的 Overlay 贴图,回退为图标本体做进度裁切
            Texture2D overlay = ModContent.RequestIfExists<Texture2D>(OverlayTexture, out var overlayAsset) ? overlayAsset.Value : sprite;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int lostHeight = (int)Math.Ceiling(overlay.Height * (1 - instance.Completion));
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, OutlineColor * opacity * 0.9f, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 分段圆弧进度环:暗色底环上,已恢复部分从顶端顺时针点亮,颜色沿弧从起始色渐变到结束色。
        /// 等效替代原灾厄 CircularBarShader 的视觉,零着色器依赖。
        /// </summary>
        public virtual void DrawProgressRing(SpriteBatch spriteBatch, Vector2 center, float opacity, float scale) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            Rectangle src = new Rectangle(0, 0, 1, 1);
            const int segments = 48;
            float radius = 19f * scale;
            float thickness = 5f * scale;
            float segLength = MathHelper.TwoPi * radius / segments + 1f;
            float recovered = 1f - instance.Completion;

            for (int i = 0; i < segments; i++) {
                float f = (i + 0.5f) / segments;
                float angle = MathHelper.TwoPi * f - MathHelper.PiOver2;
                Color color = f <= recovered
                    ? Color.Lerp(CooldownStartColor, CooldownEndColor, f)
                    : new Color(24, 24, 24);
                spriteBatch.Draw(px, center + angle.ToRotationVector2() * radius, src, color * opacity,
                    angle + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(segLength, thickness), SpriteEffects.None, 0f);
            }
        }

        /// <summary>八方向描边文本,替代原灾厄 CalamityUtils.DrawBorderStringEightWay,供护盾类冷却绘制数值。</summary>
        public static void DrawBorderStringEightWay(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 baseDrawPosition, Color main, Color border, float scale = 1f) {
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    if (x == 0 && y == 0)
                        continue;
                    spriteBatch.DrawString(font, text, baseDrawPosition + new Vector2(x, y) * scale, border, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            spriteBatch.DrawString(font, text, baseDrawPosition, main, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        #endregion
    }
}
