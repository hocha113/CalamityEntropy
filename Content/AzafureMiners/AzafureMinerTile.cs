using CalamityEntropy.Common;
using InnoVault;
using InnoVault.TileProcessors;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.AzafureMiners
{
    public class AzafureMinerTile : ModTile
    {
        public override void Load() {
            AzMinerUI.Items = new List<AzMinerUISlot>();
            AzMinerUI.Filters = new List<AzMinerUISlot>();
            for (int i = 0; i < AzMinerTP.FiltersCount; i++) {
                AzMinerUI.Filters.Add(new AzMinerUISlot() { OffsetPos = new Vector2(-168, -100 + 240f / AzMinerTP.FiltersCount * i), itemIndex = i });
            }
            int z = 0;
            Vector2 pos = new Vector2(-110, -100);
            for (int i = 0; i < AzMinerTP.ItemsCount; i++) {
                AzMinerUI.Items.Add(new AzMinerUISlot() { OffsetPos = new Vector2(pos.X, pos.Y), type = 1, itemIndex = i });
                z++;
                pos.X += 50;
                if (z >= 7) {
                    z = 0;
                    pos.Y += 50;
                    pos.X = -110;
                }
            }
        }
        public override void Unload() {
            AzMinerUI.Filters = null;
            AzMinerUI.Items = null;
        }
        public override void SetStaticDefaults() {
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileWaterDeath[Type] = false;

            AddMapEntry(new Color(190, 72, 81), VaultUtils.GetLocalizedItemName<AzafureMiner>());

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.Origin = new Point16(2, 2);
            TileObjectData.newTile.AnchorBottom = new AnchorData(
                AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide
                , TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
            TileObjectData.newTile.LavaDeath = false;

            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
        }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) {
            return true;
        }

        public override void MouseOver(int i, int j) {
            Main.LocalPlayer.SetMouseOverByTile<AzafureMiner>();
        }

        public override bool RightClick(int i, int j) {
            if (TileProcessorLoader.AutoPositionGetTP<AzMinerTP>(i, j, out var azMinerTP)) {
                if (AzMinerUI.AzMinerTP == azMinerTP) {//相同，说明是点击同一个建筑，进行开关逻辑
                    AzMinerUI.Instance.Active = !AzMinerUI.Instance.Active;
                }
                else {//不相同，说明是交叉点击不同建筑，进行切换逻辑保持开启状态
                    AzMinerUI.Instance.Active = true;
                }

                AzMinerUI.AzMinerTP = azMinerTP;
                Main.playerInventory = true;
                SoundEngine.PlaySound(SoundID.MenuOpen with { Pitch = 0.3f });
                return true;
            }
            return false;
        }
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) {
            if (!VaultUtils.SafeGetTopLeft(i, j, out var point)) {
                return false;
            }
            if (!TileProcessorLoader.ByPositionGetTP(point, out AzMinerTP azMinerTP)) {
                return false;
            }

            Tile t = Main.tile[i, j];
            int frameXPos = t.TileFrameX;
            int frameYPos = t.TileFrameY;
            Texture2D tex = TextureAssets.Tile[Type].Value;
            Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange) + azMinerTP.OffsetPos;
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + offset;
            Color drawColor = Lighting.GetColor(i, j);
            if (!azMinerTP.IsWork) {
                drawColor.R /= 2;
                drawColor.G /= 2;
                drawColor.B /= 2;
                drawColor.A = 255;
            }
            bool inArea = Main.InSmartCursorHighlightArea(azMinerTP.Position.X, azMinerTP.Position.Y, out bool slc);
            azMinerTP.SmartCursorHover = slc;
            if (inArea) {
                Color outlineColor = slc ? Color.Yellow : new Color(94, 94, 94);
                var matrix = (Matrix)Main.spriteBatch.GetType().GetField("transformMatrix", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Main.spriteBatch);
                var rasterizer = (RasterizerState)Main.spriteBatch.GetType().GetField("rasterizerState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Main.spriteBatch);
                Main.spriteBatch.End();
                EffectLoader.OutlineShader.CurrentTechnique.Passes[0].Apply();
                EffectLoader.OutlineShader.Parameters["texSize"].SetValue(tex.Size());
                EffectLoader.OutlineShader.Parameters["color"].SetValue(outlineColor.ToVector4());
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, EffectLoader.OutlineShader, Main.GameViewMatrix.TransformationMatrix);

                if (!t.IsHalfBlock && t.Slope == 0) {
                    spriteBatch.Draw(tex, drawOffset, new Rectangle(frameXPos, frameYPos, 16, 16)
                        , outlineColor, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
                }
                else if (t.IsHalfBlock) {
                    spriteBatch.Draw(tex, drawOffset + Vector2.UnitY * 8f, new Rectangle(frameXPos, frameYPos, 16, 16)
                        , outlineColor, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, rasterizer, null, matrix);
            }
            if (!t.IsHalfBlock && t.Slope == 0) {
                spriteBatch.Draw(tex, drawOffset, new Rectangle(frameXPos, frameYPos, 16, 16)
                    , drawColor, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
            }
            else if (t.IsHalfBlock) {
                spriteBatch.Draw(tex, drawOffset + Vector2.UnitY * 8f, new Rectangle(frameXPos, frameYPos, 16, 16)
                    , drawColor, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
            }
            return false;
        }
    }
}
