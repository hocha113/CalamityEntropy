using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityEntropy.Core.Cooldowns
{
    /// <summary>
    /// 冷却栏 HUD,替代原灾厄 CooldownRackUI:buff 栏下方一排图标,
    /// 少量冷却时展开显示(进度环 + 图标),超过上限自动切紧凑模式,悬停显示名称。
    /// </summary>
    public class CECooldownRackUI : ModSystem
    {
        /// <summary>展开模式最多显示的图标数,超过自动切紧凑模式。</summary>
        public const int MaxLargeIcons = 10;

        public const float CompactXSpacing = 28f;
        public const float ExpandedXSpacing = 46f;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            int buffIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Buffs");
            if (buffIndex != -1) {
                layers.Insert(buffIndex, new LegacyGameInterfaceLayer("CalamityEntropy: Cooldown Rack", () => {
                    Draw(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
            }
        }

        public static void Draw(SpriteBatch spriteBatch) {
            if (Main.gameMenu || Main.playerInventory)
                return;

            IList<CECooldownInstance> cooldownsToDraw = Main.LocalPlayer.GetDisplayedCooldowns();
            if (cooldownsToDraw.Count == 0)
                return;

            bool compact = cooldownsToDraw.Count > MaxLargeIcons;
            Vector2 spacing = Vector2.UnitX * (compact ? CompactXSpacing : ExpandedXSpacing);

            // buff 栏行数越多,冷却栏整体越往下
            float uiScale = 1f;
            Vector2 displayPosition = new Vector2(32, 100) + spacing / 2f + Vector2.UnitY * 50 * MathF.Ceiling(Main.LocalPlayer.CountBuffs() / 11f);
            int rectangleSide = (int)Math.Floor(compact ? 24 * uiScale : 52 * uiScale);
            Rectangle iconRectangle = new Rectangle((int)displayPosition.X - rectangleSide / 2, (int)displayPosition.Y - rectangleSide / 2, rectangleSide, rectangleSide);
            Rectangle mouse = new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 8, 8);

            string mouseHover = "";
            float iconOpacityScale = (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.1f + 0.6f;
            Vector2 mouseCenter = mouse.Center.ToVector2();
            float hoverOpacity = MathHelper.Clamp((float)Math.Sin(Main.GlobalTimeWrappedHourly % MathHelper.Pi) * 2f, 0, 1) * 0.1f + 0.9f;

            foreach (CECooldownInstance instance in cooldownsToDraw) {
                CECooldownHandler handler = instance.handler;
                float iconOpacity = iconOpacityScale;

                // 鼠标靠近时图标增亮
                iconOpacity += 0.3f * (1 - MathHelper.Clamp(Vector2.Distance(mouseCenter, iconRectangle.Center.ToVector2()), 0f, 80f) / 80f);

                if (iconRectangle.Intersects(mouse)) {
                    mouseHover = handler.DisplayName.ToString();
                    iconOpacity = hoverOpacity;
                }

                if (compact)
                    handler.DrawCompact(spriteBatch, displayPosition, iconOpacity, uiScale);
                else
                    handler.DrawExpanded(spriteBatch, displayPosition, iconOpacity, uiScale);

                displayPosition += spacing;
                iconRectangle.X += (int)spacing.X;
            }

            if (mouseHover != "") {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(mouseHover);
            }
        }
    }
}
