using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.UI;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    public class ArmorReforgStationTile : ModTile
    {
        public override bool IsLoadingEnabled(Mod mod) {
            return false;
        }
        public override void SetStaticDefaults() {
            RegisterItemDrop(ModContent.ItemType<ArmorReforgStation>());
            Main.tileFrameImportant[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Origin = new Point16(2, 2);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16 };
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 4, 0);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(140, 140, 140));

            DustType = DustID.Stone;
            AnimationFrameHeight = 74;
        }



        public override void AnimateTile(ref int frame, ref int frameCounter) {
            frameCounter++;
            if (frameCounter >= 7) {
                frameCounter = 0;
                if (++frame >= 4) {
                    frame = 0;
                }
            }
        }

        public override void MouseOver(int i, int j) {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;

            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<ArmorReforgStation>();
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool RightClick(int i, int j) {
            if (ArmorForgingStationUI.Visible) {
                Main.playerInventory = false;

            }
            else {
                ArmorForgingStationUI.Visible = true;
                Main.playerInventory = true;
            }
            SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
            return true;
        }


    }
}
