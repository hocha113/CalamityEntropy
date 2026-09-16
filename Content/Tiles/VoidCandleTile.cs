using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    public class VoidCandleTile : ModTile
    {
        // 脱离灾厄:原灾厄 LouderPhantomPhoenix2,按 sound-map 替换为自有音效
        public static readonly SoundStyle ActivationSound = new("CalamityEntropy/Assets/Sounds/soulScreem");

        public override void SetStaticDefaults() {
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.Origin = new Terraria.DataStructures.Point16(1, 4);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.addTile(Type);
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
            AddMapEntry(new Color(180, 40, 255), CEUtils.GetItemName<VoidCandle>());
            TileID.Sets.HasOutlines[Type] = false;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool RightClick(int i, int j) {
            Player p = Main.LocalPlayer;

            p.AddBuff(ModContent.BuffType<VoidCandleBuff>(), 108000);

            SoundEngine.PlaySound(ActivationSound, new Vector2(i * 16, j * 16));

            return true;
        }

        public override void MouseOver(int i, int j) {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<VoidCandle>();
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
            r = 0.55f;
            g = 0.1f;
            b = 0.8f;
        }
    }
}
