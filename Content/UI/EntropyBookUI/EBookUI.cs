using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Books;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace CalamityEntropy.Content.UI.EntropyBookUI
{
    public static class EBookUI
    {
        public static bool active = false;
        public static float slotRot = 3;
        public static float slotDist = 0;
        public static Item bookItem = null;
        public static int getMaxSlots(Player player, Item item) {
            int additional = player.Entropy().AdditionalBookmarkSlot;
            if (item == null || item.ModItem == null) {
                return 0;
            }
            if (item.ModItem is EntropyBook eb) {
                return eb.SlotCount + additional;
            }
            return 0;
        }
        public static void checkStackItemList() {
            if (Main.LocalPlayer.Entropy().EBookStackItems == null) {
                Main.LocalPlayer.Entropy().EBookStackItems = new List<Item>();
            }
            if (bookItem != null) {
                while (Main.LocalPlayer.Entropy().EBookStackItems.Count < getMaxSlots(Main.LocalPlayer, bookItem)) {
                    Main.LocalPlayer.Entropy().EBookStackItems.Add(new Item(ItemID.None));
                }
                if (Main.LocalPlayer.Entropy().EBookStackItems.Count > getMaxSlots(Main.LocalPlayer, bookItem)) {
                    for (int i = getMaxSlots(Main.LocalPlayer, bookItem) - 1; i >= 0; i--) {
                        if (Main.LocalPlayer.Entropy().EBookStackItems[i].type == ItemID.None) {
                            Main.LocalPlayer.Entropy().EBookStackItems.RemoveAt(i);
                        }
                    }
                }
            }
        }
        public static bool lastMouseLeft = false;
        public static bool lastMouseRight = false;
        public static int closeAnm = 0;
        public static void update() {
            checkStackItemList();
            if (!(Main.playerInventory) || Main.mouseItem.ModItem is EntropyBook) {
                active = false;
            }
            if (active) {
                closeAnm = 11;
                slotRot *= 0.88f;
                slotDist += (128 - slotDist) * 0.12f;
            }
            else {
                if (closeAnm > 0) {
                    closeAnm--;
                    slotDist *= 0.88f;
                    slotRot += (3 - slotRot) * 0.12f;
                }
                else {
                    slotRot = 3;
                    slotDist = 0;
                }
            }
            if (!Main.dedServ && !BookmarkInsertCondition.Instance.IsCompleted) {
                if (active) {
                    foreach (var i in Main.LocalPlayer.Entropy().EBookStackItems) {
                        if (BookMarkLoader.IsABookMark(i)) {
                            BookmarkInsertCondition.Instance.Complete();
                        }
                    }
                }
            }
        }
        public static List<float> OutlineAlpha = null;
        public static List<float> SlotScale = null;
        public static List<bool> LastItem = null;
        public static void draw() {
            bool sync = false;
            if (Main.LocalPlayer.Entropy().EBookStackItems == null) {
                return;
            }

            checkStackItemList();
            if (active || closeAnm > 0) {
                Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend, SamplerState.PointClamp);
                int c = getMaxSlots(Main.LocalPlayer, bookItem);
                if (OutlineAlpha == null)
                    OutlineAlpha = new();
                while (OutlineAlpha.Count < c)
                    OutlineAlpha.Add(0);
                if (SlotScale == null)
                    SlotScale = new();
                while (SlotScale.Count < c)
                    SlotScale.Add(1);
                if (LastItem == null)
                    LastItem = new();
                while (LastItem.Count < c) {
                    LastItem.Add(false);
                }
                List<Texture2D> texSpecial = Main.LocalPlayer.Entropy().BookmarkHolderSpecialTextures;
                bool outlineFlag = false;
                for (int i = 0; i < c; i++) {
                    float drawScale = SlotScale[i];
                    bool Holding = false;
                    Texture2D holderTexture = null;
                    Vector2 pos = Main.ScreenSize.ToVector2() / 2 + (MathHelper.ToRadians(i * (360f / c)) + slotRot).ToRotationVector2() * slotDist;
                    if (!(bookItem.ModItem is EntropyBook)) {
                        active = false;
                        return;
                    }
                    EntropyBook eb = (EntropyBook)bookItem.ModItem;
                    holderTexture = eb.BookMarkTexture;
                    if (i >= c - texSpecial.Count) {
                        holderTexture = texSpecial[i - (c - texSpecial.Count)];
                    }
                    bool predraw = eb.PreDrawBookmarkSlot(Main.LocalPlayer.Entropy().EBookStackItems[i], pos, (closeAnm / 11f), drawScale, OutlineAlpha[i]);


                    if (predraw) {
                        //Holder
                        Main.spriteBatch.Draw(holderTexture, pos, null, Color.White * (closeAnm / 11f), 0, holderTexture.Size() / 2, 1 * drawScale, SpriteEffects.None, 0);

                        //Bookmark
                        if (Main.LocalPlayer.Entropy().EBookStackItems.Count > i && Main.LocalPlayer.Entropy().EBookStackItems[i].type != ItemID.None) {
                            if (BookMarkLoader.IsABookMark(Main.LocalPlayer.Entropy().EBookStackItems[i]) && BookMarkLoader.GetUITexture(Main.LocalPlayer.Entropy().EBookStackItems[i]) != null) {
                                Main.spriteBatch.Draw(BookMarkLoader.GetUITexture(Main.LocalPlayer.Entropy().EBookStackItems[i]), pos, null, Color.White * (closeAnm / 11f), 0, BookMarkLoader.GetUITexture(Main.LocalPlayer.Entropy().EBookStackItems[i]).Size() / 2, 1 * drawScale, SpriteEffects.None, 0);
                            }
                            else {
                                ItemSlot.DrawItemIcon(Main.LocalPlayer.Entropy().EBookStackItems[i], 1, Main.spriteBatch, pos, 0.8f * drawScale, 256, Color.White * (closeAnm / 11f));
                            }
                        }
                    }

                    if (!outlineFlag && active && Main.MouseScreen.getRectCentered(2, 2).Intersects(pos.getRectCentered(36, 46))) {
                        if (!Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir) {
                            CEUtils.showItemTooltip(Main.LocalPlayer.Entropy().EBookStackItems[i]);
                        }
                        Main.LocalPlayer.mouseInterface = true;
                        bool mlAndShift = Main.mouseLeft && !lastMouseLeft && Keyboard.GetState().IsKeyDown(Keys.LeftShift);
                        if (!mlAndShift && Main.mouseLeft && !lastMouseLeft && (Main.mouseItem.IsAir || BookMarkLoader.IsABookMark(Main.mouseItem)) && !(Main.mouseItem.IsAir && Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir)) {
                            bool flag = true;
                            if (!Main.mouseItem.IsAir) {
                                for (int h = 0; h < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, bookItem), Main.LocalPlayer.Entropy().EBookStackItems.Count); h++) {
                                    if (BookMarkLoader.IsABookMark(Main.LocalPlayer.Entropy().EBookStackItems[h])) {
                                        var bm = Main.LocalPlayer.Entropy().EBookStackItems[h];
                                        if (!BookMarkLoader.CanBeEquipWith(Main.mouseItem, bm)) {
                                            flag = false;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (flag) {
                                lastMouseLeft = true;
                                Item mouseItem = Main.mouseItem.Clone();
                                Main.mouseItem = Main.LocalPlayer.Entropy().EBookStackItems[i];
                                Main.LocalPlayer.Entropy().EBookStackItems[i] = mouseItem;
                                eb.PlayBookmarkInsertSound();
                                sync = true;
                            }
                        }
                        if (((Main.mouseRight && !lastMouseRight) || mlAndShift) && slotDist > 100) {
                            if (mlAndShift) {
                                lastMouseLeft = true;
                            }
                            if (!Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir) {
                                for (int ii = 10; ii < 50; ii++) {
                                    if (Main.LocalPlayer.inventory[ii].IsAir) {
                                        ItemIO.Load(Main.LocalPlayer.inventory[ii], ItemIO.Save(Main.LocalPlayer.Entropy().EBookStackItems[i]));
                                        Main.LocalPlayer.Entropy().EBookStackItems[i].TurnToAir();
                                        eb.PlayBookmarkInsertSound();
                                        sync = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (Main.mouseItem.IsAir && Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir) {
                            Main.instance.MouseText(CalamityEntropy.Instance.GetLocalization("SlotInfo").Value);
                        }
                        if (Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir || (!Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir && (Main.mouseItem.IsAir || BookMarkLoader.IsABookMark(Main.mouseItem)))) {
                            Holding = true;
                            outlineFlag = true;
                        }
                    }
                    if (!Holding) {
                        OutlineAlpha[i] += Utils.Remap(pos.Distance(Main.MouseScreen), 0, 140, 0.3f, 0);
                        SlotScale[i] += Utils.Remap(pos.Distance(Main.MouseScreen), 0, 140, 0.03f, 0);
                    }
                    OutlineAlpha[i] = float.Lerp(OutlineAlpha[i], Holding ? 1 : 0, 0.4f);
                    SlotScale[i] = float.Lerp(SlotScale[i], 1, 0.2f);
                    if (OutlineAlpha[i] >= 0.005f) {
                        if (!Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir)
                            ItemSlot.DrawItemIcon(Main.LocalPlayer.Entropy().EBookStackItems[i], 1, Main.spriteBatch, pos + new Vector2(0, -8 - OutlineAlpha[i] * 36), 0.85f * OutlineAlpha[i], 256, Color.White * (closeAnm / 11f) * OutlineAlpha[i]);

                        if (shader != null && predraw) {
                            Main.spriteBatch.End();
                            shader.CurrentTechnique.Passes[0].Apply();
                            shader.Parameters["texSize"].SetValue(holderTexture.Size());
                            shader.Parameters["color"].SetValue(Color.Yellow.ToVector4());
                            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.UIScaleMatrix);
                            Main.spriteBatch.Draw(holderTexture, pos, null, Color.White * (closeAnm / 11f) * OutlineAlpha[i], 0, holderTexture.Size() / 2, 1 * drawScale, SpriteEffects.None, 0);

                            Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend, SamplerState.PointClamp);

                        }
                    }
                    if (Holding && (BookMarkLoader.IsABookMark(Main.mouseItem) || Main.mouseItem.IsAir))
                        SlotScale[i] += 0.04f;
                    if (LastItem[i] != !Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir) {
                        SlotScale[i] = Holding ? 1f : 0.8f;
                    }
                    LastItem[i] = !Main.LocalPlayer.Entropy().EBookStackItems[i].IsAir;
                }

                Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
                lastMouseLeft = Main.mouseLeft;
                lastMouseRight = Main.mouseRight;
                if (sync && Main.netMode != NetmodeID.SinglePlayer) {
                    Main.LocalPlayer.Entropy().SyncBookmarks();
                }
            }
        }
        public static Effect shader = null;
    }
}