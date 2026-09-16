using InnoVault;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace CalamityEntropy.Content.AzafureMiners
{
    public class AzMinerUI : UIHandle
    {
        public static AzMinerUI Instance => UIHandleLoader.GetUIHandleOfType<AzMinerUI>();
        public static AzMinerTP AzMinerTP { get; set; } = null;
        public static List<AzMinerUISlot> Filters { get; set; }
        public static List<AzMinerUISlot> Items { get; set; }

        //面板尺寸
        private const float PanelWidth = 420f;
        private const float PanelHeight = 320f;
        private const float TitleBarHeight = 36f;

        //动画计时器
        private float scanLineTimer = 0f;
        private float heatPulseTimer = 0f;
        private float dataStreamTimer = 0f;
        private float sparkTimer = 0f;
        private float borderGlowTimer = 0f;

        //粒子系统
        private readonly List<TechParticle> techParticles = [];
        private int particleSpawnTimer = 0;
        private readonly List<EmberParticle> embers = [];
        private int emberSpawnTimer = 0;

        //UI状态
        internal static float uiAlpha = 0f;
        private static bool IsActive;
        private bool isDragging = false;
        private Vector2 dragOffset = new Vector2(800, 440);
        internal int dontDragTime;
        private float hoverProgress = 0f;

        //区域定义
        private Rectangle panelRect;
        private Rectangle titleBarRect;

        public override bool Active {
            get {
                if (AzMinerTP == null || !AzMinerTP.Active) {
                    IsActive = false;
                }
                return IsActive || uiAlpha > 0.01f;
            }
            set => IsActive = value;
        }
        public static bool InitDragPos = true;

        public override void Update() {
            if (InitDragPos) {
                InitDragPos = false;
                DrawPosition = new Vector2(800, 440);
            }
            if (dontDragTime > 0) {
                dontDragTime--;
            }

            //更新UI透明度
            float targetAlpha = IsActive ? 1f : 0f;
            uiAlpha = MathHelper.Lerp(uiAlpha, targetAlpha, 0.4f);

            if (uiAlpha < 0.1f && !IsActive) {
                return;
            }

            //更新动画计时器
            UpdateAnimationTimers();

            //更新粒子
            UpdateParticles();

            if (AzMinerTP == null) {
                return;
            }

            //先处理拖拽，这样位置更新后所有元素都使用新位置
            HandleDragging();

            //限制面板位置在屏幕内
            DrawPosition.X = MathHelper.Clamp(DrawPosition.X, PanelWidth / 2 + 10, Main.screenWidth - PanelWidth / 2 - 10);
            DrawPosition.Y = MathHelper.Clamp(DrawPosition.Y, PanelHeight / 2 + 10, Main.screenHeight - PanelHeight / 2 - 10);

            //计算面板区域，在位置更新后计算
            Vector2 topLeft = DrawPosition - new Vector2(PanelWidth / 2, PanelHeight / 2);
            panelRect = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)PanelWidth + 20, (int)PanelHeight);
            titleBarRect = new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, (int)TitleBarHeight);

            UIHitBox = panelRect;
            hoverInMainPage = panelRect.Contains(MouseHitBox);

            //更新悬停进度
            if (hoverInMainPage) {
                hoverProgress = Math.Min(1f, hoverProgress + 0.08f);
            }
            else {
                hoverProgress = Math.Max(0f, hoverProgress - 0.06f);
            }

            //检查是否应该关闭UI
            if (!Main.playerInventory || AzMinerTP.CenterInWorld.Distance(Main.LocalPlayer.Center) > 20 * 16) {
                IsActive = false;
                return;
            }

            //更新槽位，位置已经是最新的
            if (Filters != null) {
                foreach (AzMinerUISlot s in Filters) {
                    s.Update();
                }
            }
            if (Items != null) {
                foreach (AzMinerUISlot s in Items) {
                    s.Update();
                }
            }

            if (hoverInMainPage) {
                player.mouseInterface = true;
            }
        }

        private void UpdateAnimationTimers() {
            scanLineTimer += 0.04f;
            heatPulseTimer += 0.025f;
            dataStreamTimer += 0.05f;
            sparkTimer += 0.08f;
            borderGlowTimer += 0.035f;

            if (scanLineTimer > MathHelper.TwoPi) scanLineTimer -= MathHelper.TwoPi;
            if (heatPulseTimer > MathHelper.TwoPi) heatPulseTimer -= MathHelper.TwoPi;
            if (dataStreamTimer > MathHelper.TwoPi) dataStreamTimer -= MathHelper.TwoPi;
            if (sparkTimer > MathHelper.TwoPi) sparkTimer -= MathHelper.TwoPi;
            if (borderGlowTimer > MathHelper.TwoPi) borderGlowTimer -= MathHelper.TwoPi;
        }

        private void UpdateParticles() {
            if (uiAlpha < 0.3f) return;

            Vector2 panelCenter = DrawPosition;

            //科技粒子
            particleSpawnTimer++;
            if (IsActive && particleSpawnTimer >= 15 && techParticles.Count < 12) {
                particleSpawnTimer = 0;
                float xPos = Main.rand.NextFloat(panelCenter.X - PanelWidth / 2 + 30, panelCenter.X + PanelWidth / 2 - 30);
                float yPos = Main.rand.NextFloat(panelCenter.Y - PanelHeight / 2 + 50, panelCenter.Y + PanelHeight / 2 - 30);
                techParticles.Add(new TechParticle(new Vector2(xPos, yPos)));
            }
            for (int i = techParticles.Count - 1; i >= 0; i--) {
                if (techParticles[i].Update()) {
                    techParticles.RemoveAt(i);
                }
            }

            //余烬粒子，采矿机工作时生成
            if (AzMinerTP != null && AzMinerTP.IsWork) {
                emberSpawnTimer++;
                if (emberSpawnTimer >= 6 && embers.Count < 25) {
                    emberSpawnTimer = 0;
                    float xPos = Main.rand.NextFloat(panelCenter.X - PanelWidth / 2 + 40, panelCenter.X + PanelWidth / 2 - 40);
                    Vector2 startPos = new(xPos, panelCenter.Y + PanelHeight / 2 - 20);
                    embers.Add(new EmberParticle(startPos));
                }
            }
            for (int i = embers.Count - 1; i >= 0; i--) {
                if (embers[i].Update()) {
                    embers.RemoveAt(i);
                }
            }
        }

        private void HandleDragging() {
            //检查鼠标是否在任何槽位上
            bool hoveringAnySlot = false;
            if (Filters != null) {
                foreach (var slot in Filters) {
                    Rectangle slotRect = new Rectangle(
                        (int)(DrawPosition.X + slot.OffsetPos.X - 22),
                        (int)(DrawPosition.Y + slot.OffsetPos.Y - 22),
                        44, 44);
                    if (slotRect.Contains(MouseHitBox)) {
                        hoveringAnySlot = true;
                        break;
                    }
                }
            }
            if (!hoveringAnySlot && Items != null) {
                foreach (var slot in Items) {
                    Rectangle slotRect = new Rectangle(
                        (int)(DrawPosition.X + slot.OffsetPos.X - 22),
                        (int)(DrawPosition.Y + slot.OffsetPos.Y - 22),
                        44, 44);
                    if (slotRect.Contains(MouseHitBox)) {
                        hoveringAnySlot = true;
                        break;
                    }
                }
            }

            //整个面板都可以拖拽，但排除槽位区域
            bool hoveringPanel = panelRect.Contains(MouseHitBox);
            bool canStartDrag = hoveringPanel && !hoveringAnySlot && dontDragTime <= 0;

            if (canStartDrag && keyLeftPressState == KeyPressState.Pressed) {
                isDragging = true;
                dragOffset = DrawPosition - MousePosition;
            }

            if (isDragging) {
                player.mouseInterface = true;
                DrawPosition = MousePosition + dragOffset;

                if (keyLeftPressState == KeyPressState.Released) {
                    isDragging = false;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (uiAlpha < 0.01f) return;
            if (AzMinerTP == null) return;

            //绘制主面板
            DrawMainPanel(spriteBatch);

            //绘制粒子
            DrawParticles(spriteBatch);

            //绘制标题栏
            DrawTitleBar(spriteBatch);

            //绘制状态指示器
            DrawStatusIndicator(spriteBatch);

            //绘制槽位
            if (Filters != null) {
                foreach (var slot in Filters) {
                    slot.Draw(spriteBatch);
                }
            }
            if (Items != null) {
                foreach (var slot in Items) {
                    slot.Draw(spriteBatch);
                }
            }

            //绘制扫描线效果
            DrawScanLines(spriteBatch);
        }

        private void DrawMainPanel(SpriteBatch sb) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            float alpha = uiAlpha;

            //深度阴影
            Rectangle shadowRect = panelRect;
            shadowRect.Offset(6, 8);
            sb.Draw(pixel, shadowRect, new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.5f));

            //主背景，深红棕色废土风格
            Color bgColor = new Color(22, 14, 12) * (alpha * 0.95f);
            sb.Draw(pixel, panelRect, new Rectangle(0, 0, 1, 1), bgColor);

            //内层渐变背景
            Rectangle innerRect = panelRect;
            innerRect.Inflate(-4, -4);
            Color innerBg = new Color(35, 20, 18) * (alpha * 0.85f);
            sb.Draw(pixel, innerRect, new Rectangle(0, 0, 1, 1), innerBg);

            //边框脉冲效果
            float pulse = (float)Math.Sin(borderGlowTimer) * 0.3f + 0.7f;
            Color borderColor = new Color(180, 80, 60) * (alpha * pulse);

            //外边框
            sb.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 3), new Rectangle(0, 0, 1, 1), borderColor);
            sb.Draw(pixel, new Rectangle(panelRect.X, panelRect.Bottom - 3, panelRect.Width, 3), new Rectangle(0, 0, 1, 1), borderColor * 0.8f);
            sb.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y, 3, panelRect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.9f);
            sb.Draw(pixel, new Rectangle(panelRect.Right - 3, panelRect.Y, 3, panelRect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.9f);

            //内发光边框
            Rectangle innerBorder = panelRect;
            innerBorder.Inflate(-6, -6);
            Color innerGlow = new Color(255, 120, 80) * (alpha * 0.15f * pulse);
            sb.Draw(pixel, new Rectangle(innerBorder.X, innerBorder.Y, innerBorder.Width, 2), new Rectangle(0, 0, 1, 1), innerGlow);
            sb.Draw(pixel, new Rectangle(innerBorder.X, innerBorder.Bottom - 2, innerBorder.Width, 2), new Rectangle(0, 0, 1, 1), innerGlow * 0.7f);
            sb.Draw(pixel, new Rectangle(innerBorder.X, innerBorder.Y, 2, innerBorder.Height), new Rectangle(0, 0, 1, 1), innerGlow * 0.8f);
            sb.Draw(pixel, new Rectangle(innerBorder.Right - 2, innerBorder.Y, 2, innerBorder.Height), new Rectangle(0, 0, 1, 1), innerGlow * 0.8f);

            //角落装饰
            DrawCornerDecoration(sb, new Vector2(panelRect.X, panelRect.Y), alpha, 0);
            DrawCornerDecoration(sb, new Vector2(panelRect.Right, panelRect.Y), alpha, 1);
            DrawCornerDecoration(sb, new Vector2(panelRect.X, panelRect.Bottom), alpha, 2);
            DrawCornerDecoration(sb, new Vector2(panelRect.Right, panelRect.Bottom), alpha, 3);
        }

        private void DrawCornerDecoration(SpriteBatch sb, Vector2 pos, float alpha, int corner) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Color decorColor = new Color(200, 100, 70) * (alpha * 0.6f);
            int size = 12;
            int thickness = 2;

            switch (corner) {
                case 0: //左上
                    sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, size, thickness), new Rectangle(0, 0, 1, 1), decorColor);
                    sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, thickness, size), new Rectangle(0, 0, 1, 1), decorColor);
                    break;
                case 1: //右上
                    sb.Draw(pixel, new Rectangle((int)pos.X - size, (int)pos.Y, size, thickness), new Rectangle(0, 0, 1, 1), decorColor);
                    sb.Draw(pixel, new Rectangle((int)pos.X - thickness, (int)pos.Y, thickness, size), new Rectangle(0, 0, 1, 1), decorColor);
                    break;
                case 2: //左下
                    sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y - thickness, size, thickness), new Rectangle(0, 0, 1, 1), decorColor);
                    sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y - size, thickness, size), new Rectangle(0, 0, 1, 1), decorColor);
                    break;
                case 3: //右下
                    sb.Draw(pixel, new Rectangle((int)pos.X - size, (int)pos.Y - thickness, size, thickness), new Rectangle(0, 0, 1, 1), decorColor);
                    sb.Draw(pixel, new Rectangle((int)pos.X - thickness, (int)pos.Y - size, thickness, size), new Rectangle(0, 0, 1, 1), decorColor);
                    break;
            }
        }

        private void DrawTitleBar(SpriteBatch sb) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            float alpha = uiAlpha;

            //标题栏背景
            Color titleBg = new Color(45, 25, 22) * (alpha * 0.9f);
            sb.Draw(pixel, titleBarRect, new Rectangle(0, 0, 1, 1), titleBg);

            //标题栏底部分隔线
            float pulse = (float)Math.Sin(heatPulseTimer * 2f) * 0.3f + 0.7f;
            Color separatorColor = new Color(200, 90, 60) * (alpha * pulse * 0.8f);
            sb.Draw(pixel, new Rectangle(titleBarRect.X + 8, titleBarRect.Bottom - 2, titleBarRect.Width - 16, 2), new Rectangle(0, 0, 1, 1), separatorColor);

            //标题文本
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string title = VaultUtils.GetLocalizedItemName<AzafureMiner>().Value;
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(titleBarRect.X + (titleBarRect.Width - titleSize.X) / 2, titleBarRect.Y + (titleBarRect.Height - titleSize.Y) / 2);

            //标题发光
            Color glowColor = new Color(255, 150, 100) * (alpha * 0.5f);
            for (int i = 0; i < 4; i++) {
                float angle = MathHelper.TwoPi * i / 4f;
                Vector2 offset = angle.ToRotationVector2() * 2f;
                sb.DrawString(font, title, titlePos + offset, glowColor);
            }
            sb.DrawString(font, title, titlePos, new Color(255, 220, 200) * alpha);
        }

        private void DrawStatusIndicator(SpriteBatch sb) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            float alpha = uiAlpha;

            //状态指示灯位置
            Vector2 indicatorPos = new Vector2(panelRect.Right - 25, titleBarRect.Y + titleBarRect.Height / 2);

            //指示灯背景
            Color bgColor = new Color(20, 12, 10) * alpha;
            Rectangle bgRect = new Rectangle((int)indicatorPos.X - 8, (int)indicatorPos.Y - 8, 16, 16);
            sb.Draw(pixel, bgRect, new Rectangle(0, 0, 1, 1), bgColor);

            //指示灯颜色，根据工作状态
            bool isWorking = AzMinerTP?.IsWork ?? false;
            float pulse = (float)Math.Sin(sparkTimer * (isWorking ? 3f : 1f)) * 0.3f + 0.7f;
            Color lightColor = isWorking ? new Color(100, 255, 100) : new Color(255, 180, 80);
            lightColor *= alpha * pulse;

            Rectangle lightRect = new Rectangle((int)indicatorPos.X - 5, (int)indicatorPos.Y - 5, 10, 10);
            sb.Draw(pixel, lightRect, new Rectangle(0, 0, 1, 1), lightColor);

            //发光效果
            if (isWorking) {
                Color glowCol = new Color(100, 255, 100) * (alpha * 0.3f * pulse);
                Rectangle glowRect = new Rectangle((int)indicatorPos.X - 8, (int)indicatorPos.Y - 8, 16, 16);
                sb.Draw(pixel, glowRect, new Rectangle(0, 0, 1, 1), glowCol);
            }
        }

        private void DrawScanLines(SpriteBatch sb) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            float alpha = uiAlpha * 0.08f;

            //扫描线效果
            float scanY = panelRect.Y + (float)Math.Sin(scanLineTimer) * 0.5f * panelRect.Height + panelRect.Height * 0.5f;
            Color scanColor = new Color(255, 150, 100) * alpha;

            for (int i = 0; i < 3; i++) {
                float y = scanY + i * 3;
                if (y > panelRect.Y && y < panelRect.Bottom) {
                    sb.Draw(pixel, new Rectangle(panelRect.X + 10, (int)y, panelRect.Width - 20, 1), new Rectangle(0, 0, 1, 1), scanColor * (1f - i * 0.3f));
                }
            }
        }

        private void DrawParticles(SpriteBatch sb) {
            float alpha = uiAlpha;

            foreach (var particle in techParticles) {
                particle.Draw(sb, alpha);
            }
            foreach (var ember in embers) {
                ember.Draw(sb, alpha);
            }
        }

        #region 粒子类
        private class TechParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Rotation;

            public TechParticle(Vector2 pos) {
                Position = pos;
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(0.2f, 0.8f);
                Velocity = angle.ToRotationVector2() * speed;
                Life = 0f;
                MaxLife = Main.rand.NextFloat(40f, 80f);
                Size = Main.rand.NextFloat(1f, 2.5f);
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }

            public bool Update() {
                Life++;
                Position += Velocity;
                Velocity *= 0.98f;
                Rotation += 0.02f;
                return Life >= MaxLife;
            }

            public void Draw(SpriteBatch sb, float alpha) {
                float t = Life / MaxLife;
                float fade = (float)Math.Sin(t * MathHelper.Pi) * alpha;

                Texture2D pixel = VaultAsset.placeholder2.Value;
                Color color = new Color(255, 140, 100) * (0.5f * fade);

                sb.Draw(pixel, Position, new Rectangle(0, 0, 1, 1), color, Rotation, new Vector2(0.5f), new Vector2(Size * 2f, Size * 0.4f), SpriteEffects.None, 0f);
            }
        }

        private class EmberParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;

            public EmberParticle(Vector2 pos) {
                Position = pos;
                Velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.5f, -0.8f));
                Life = 0f;
                MaxLife = Main.rand.NextFloat(30f, 60f);
                Size = Main.rand.NextFloat(1.5f, 3f);
            }

            public bool Update() {
                Life++;
                Position += Velocity;
                Velocity.X += Main.rand.NextFloat(-0.05f, 0.05f);
                Velocity.Y *= 0.99f;
                return Life >= MaxLife;
            }

            public void Draw(SpriteBatch sb, float alpha) {
                float t = Life / MaxLife;
                float fade = (float)Math.Sin(t * MathHelper.Pi) * alpha;

                Texture2D pixel = VaultAsset.placeholder2.Value;
                Color color = Color.Lerp(new Color(255, 200, 100), new Color(255, 80, 40), t) * fade;

                sb.Draw(pixel, Position, new Rectangle(0, 0, 1, 1), color, 0f, new Vector2(0.5f), Size * (1f - t * 0.5f), SpriteEffects.None, 0f);
            }
        }
        #endregion
    }

    /// <summary>
    /// 采矿机UI槽位
    /// </summary>
    public class AzMinerUISlot : UIHandle
    {
        public override LayersModeEnum LayersMode => LayersModeEnum.None;
        public int itemIndex = 0;
        public int type = 0; //0=过滤槽，1=物品槽
        public Vector2 OffsetPos = new Vector2();

        private const int SlotSize = 44;
        private float hoverProgress = 0f;
        private bool hoverRightHeld;
        private int lastHoverSlot = -1;

        public Item Item {
            get {
                if (type == 0) {
                    return AzMinerUI.AzMinerTP.filters[itemIndex];
                }
                else {
                    return AzMinerUI.AzMinerTP.items[itemIndex];
                }
            }
            set {
                if (type == 0) {
                    AzMinerUI.AzMinerTP.filters[itemIndex] = value;
                }
                else {
                    AzMinerUI.AzMinerTP.items[itemIndex] = value;
                }
            }
        }

        public override void Update() {
            //更新位置和鼠标交互区域
            DrawPosition = AzMinerUI.Instance.DrawPosition + OffsetPos;
            UIHitBox = new Rectangle((int)(DrawPosition.X - SlotSize / 2), (int)(DrawPosition.Y - SlotSize / 2), SlotSize, SlotSize);

            hoverInMainPage = UIHitBox.Contains(MouseHitBox);

            if (!hoverInMainPage) {
                hoverProgress = Math.Max(0f, hoverProgress - 0.1f);
                hoverRightHeld = false;
                return;
            }
            hoverProgress = Math.Min(1f, hoverProgress + 0.12f);

            Main.LocalPlayer.mouseInterface = true;

            //显示物品信息
            if (Main.mouseItem.IsAir && !Item.IsAir) {
                CEUtils.showItemTooltip(Item);
            }

            //空槽提示
            if (Item.IsAir && type == 0 && Main.mouseItem.IsAir) {
                Main.instance.MouseText(CalamityEntropy.Instance.GetLocalization("SlotInfo2").Value);
            }

            //Shift合并
            if (!Item.IsAir && Main.keyState.PressingShift()) {
                TryMergeStacks();
            }

            //空物品格直接退出
            if (Main.mouseItem.IsAir && Item.IsAir) {
                return;
            }

            //鼠标右键操作
            HandleRightClick();

            //鼠标左键操作
            HandleLeftClick();

            AzMinerUI.AzMinerTP.SendData();
        }

        private void HandleRightClick() {
            int currentSlot = itemIndex;

            if (currentSlot != lastHoverSlot) {
                hoverRightHeld = false;
                lastHoverSlot = currentSlot;
            }

            if (Main.mouseItem.IsAir) {
                return;
            }

            bool canAdd = Item.IsAir || Item.type == Main.mouseItem.type && Item.stack < Item.maxStack && Main.mouseItem.stack > 0;

            if (!canAdd) {
                return;
            }

            if (keyRightPressState == KeyPressState.Held && hoverInMainPage) {
                if (!hoverRightHeld) {
                    AddOneFromMouse();
                    hoverRightHeld = true;
                }
            }
            else if (keyRightPressState == KeyPressState.Pressed) {
                AddOneFromMouse();
                hoverRightHeld = true;
            }
        }

        private void HandleLeftClick() {
            if (keyLeftPressState != KeyPressState.Pressed)
                return;

            AzMinerUI.Instance.dontDragTime = 2;

            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift)) {
                SoundEngine.PlaySound(SoundID.Grab);
                Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Loot(), Item, Item.stack);
                ClearSlot();
                return;
            }

            if (Main.mouseItem.type == Item.type) {
                MergeWithMouse();
            }
            else {
                if (!IsOre(Main.mouseItem) && Main.mouseItem.type != ItemID.None) {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    Main.NewText(CalamityEntropy.Instance.GetLocalization("AzMinerOreWarning").Value, Color.Red);
                    return;
                }
                SwapWithMouse();
            }
        }

        private void AddOneFromMouse() {
            SoundEngine.PlaySound(SoundID.Grab);
            if (Item.IsAir) {
                Item = Main.mouseItem.Clone();
                Item.stack = 1;
            }
            else {
                Item.stack++;
            }

            Main.mouseItem.stack--;
            if (Main.mouseItem.stack <= 0)
                Main.mouseItem.TurnToAir();
        }

        private void MergeWithMouse() {
            SoundEngine.PlaySound(SoundID.Grab);
            Item targetSlot = GetSlotRef();
            int total = targetSlot.stack + Main.mouseItem.stack;

            if (total > targetSlot.maxStack) {
                int overflow = total - targetSlot.maxStack;
                targetSlot.stack = targetSlot.maxStack;
                Main.mouseItem.stack = overflow;
            }
            else {
                targetSlot.stack = total;
                Main.mouseItem.TurnToAir();
            }
        }

        private void SwapWithMouse() {
            SoundEngine.PlaySound(SoundID.Grab);
            Item targetSlot = GetSlotRef();

            Item temp = targetSlot.Clone();
            targetSlot.SetDefaults(Main.mouseItem.type);
            targetSlot.stack = Main.mouseItem.stack;

            Main.mouseItem = temp;
        }

        private void TryMergeStacks() {
            bool merged = false;
            foreach (var slot in AzMinerUI.Items) {
                if (slot.itemIndex == itemIndex || Item.type != slot.Item.type)
                    continue;

                if (Item.stack >= Item.maxStack)
                    continue;

                int total = Item.stack + slot.Item.stack;
                if (total > Item.maxStack) {
                    Item.stack = Item.maxStack;
                    slot.Item.stack = total - Item.maxStack;
                }
                else {
                    Item.stack = total;
                    slot.Item.TurnToAir();
                }
                merged = true;
            }

            if (merged)
                SoundEngine.PlaySound(SoundID.Grab);
        }

        private void ClearSlot() {
            if (type == 0) {
                AzMinerUI.AzMinerTP.filters[itemIndex].TurnToAir();
            }
            else {
                AzMinerUI.AzMinerTP.items[itemIndex].TurnToAir();
            }
        }

        private Item GetSlotRef() {
            return type == 0
                ? AzMinerUI.AzMinerTP.filters[itemIndex]
                : AzMinerUI.AzMinerTP.items[itemIndex];
        }

        private bool IsOre(Item item) {
            return AzMinerTP.ItemIsOre.TryGetValue(item.type, out bool isOre) && isOre;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            float alpha = AzMinerUI.uiAlpha;
            if (alpha < 0.01f) return;

            Texture2D pixel = VaultAsset.placeholder2.Value;
            Vector2 center = DrawPosition;

            //槽位背景
            float scale = 1f + hoverProgress * 0.08f;
            int scaledSize = (int)(SlotSize * scale);
            Rectangle slotRect = new Rectangle((int)(center.X - scaledSize / 2), (int)(center.Y - scaledSize / 2), scaledSize, scaledSize);

            //背景色，过滤槽和物品槽颜色略有不同
            Color bgColor = type == 0 ? new Color(50, 30, 28) : new Color(35, 22, 20);
            bgColor *= alpha * 0.9f;
            spriteBatch.Draw(pixel, slotRect, new Rectangle(0, 0, 1, 1), bgColor);

            //边框
            Color borderColor = type == 0 ? new Color(200, 120, 80) : new Color(160, 90, 60);
            borderColor *= alpha * (0.6f + hoverProgress * 0.4f);

            spriteBatch.Draw(pixel, new Rectangle(slotRect.X, slotRect.Y, slotRect.Width, 2), new Rectangle(0, 0, 1, 1), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(slotRect.X, slotRect.Bottom - 2, slotRect.Width, 2), new Rectangle(0, 0, 1, 1), borderColor * 0.8f);
            spriteBatch.Draw(pixel, new Rectangle(slotRect.X, slotRect.Y, 2, slotRect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.9f);
            spriteBatch.Draw(pixel, new Rectangle(slotRect.Right - 2, slotRect.Y, 2, slotRect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.9f);

            //悬停发光效果
            if (hoverProgress > 0.01f) {
                Color glowColor = new Color(255, 180, 120) * (alpha * hoverProgress * 0.2f);
                Rectangle glowRect = slotRect;
                glowRect.Inflate(2, 2);
                spriteBatch.Draw(pixel, glowRect, new Rectangle(0, 0, 1, 1), glowColor);
            }

            //绘制物品
            if (!Item.IsAir) {
                float itemScale = scale * (0.9f + hoverProgress * 0.1f);
                ItemSlot.DrawItemIcon(Item, 1, spriteBatch, center, itemScale, 128, Color.White * alpha);

                //物品数量
                if (Item.stack > 1) {
                    DynamicSpriteFont font = FontAssets.ItemStack.Value;
                    string stackText = Item.stack.ToString();
                    Vector2 stackPos = center + new Vector2(SlotSize / 2 - 18, SlotSize / 2 - 16) * scale;

                    //数量阴影
                    spriteBatch.DrawString(font, stackText, stackPos + new Vector2(1, 1), Color.Black * alpha * 0.8f, 0, Vector2.Zero, 0.75f * scale, SpriteEffects.None, 0);
                    spriteBatch.DrawString(font, stackText, stackPos, Color.White * alpha, 0, Vector2.Zero, 0.75f * scale, SpriteEffects.None, 0);
                }
            }
            else if (type == 0) {
                //空过滤槽显示提示图标
                Color hintColor = new Color(150, 100, 80) * (alpha * 0.4f);
                DynamicSpriteFont font = FontAssets.MouseText.Value;
                string hint = "?";
                Vector2 hintSize = font.MeasureString(hint);
                spriteBatch.DrawString(font, hint, center - hintSize / 2, hintColor);
            }
        }
    }
}
