using CalamityEntropy.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    public class GiantBoulderTile : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileFrameImportant[(int)base.Type] = true;
            Main.tileSolid[(int)base.Type] = true;
            Main.tileNoAttach[(int)base.Type] = false;
            Main.tileBlockLight[(int)base.Type] = false;
            TileID.Sets.Boulders[(int)base.Type] = true;
            TileID.Sets.DrawsWalls[(int)base.Type] = true;
            TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[(int)base.Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 10;
            TileObjectData.newTile.Height = 10;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16, 16, 16, 16, 16, 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.Origin = new Point16(4, 9);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile((int)base.Type);
            base.HitSound = SoundID.Dig;
            base.DustType = DustID.Stone;
            AddMapEntry(Color.DarkGray, CreateMapEntryName());
            MineResist = 0.01f;
            MinPick = 0;
        }

        public override bool IsTileDangerous(int i, int j, Player player) {
            return true;
        }

        public override bool Slope(int i, int j) {
            return false;
        }
        public override bool CanExplode(int i, int j) {
            return false;
        }
        public override void KillMultiTile(int i, int j, int frameX, int frameY) {
            Projectile.NewProjectile(new EntitySource_TileBreak(i, j, null), new Vector2((float)(i + 5), (float)(j + 5)) * 16f, Vector2.Zero, ModContent.ProjectileType<GiantBoulderProj>(), 600, 0f, -1, 0f, 0f, 0f);
        }
        public override IEnumerable<Item> GetItemDrops(int i, int j) {
            yield return new Item(0, 1, 0);
            yield break;
        }
    }
}
