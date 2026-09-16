using CalamityEntropy.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    public class AuricBoulderTile : ModTile
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
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile((int)base.Type);
            // 脱离灾厄:原灾厄 AuricMine,按 sound-map 替换
            base.HitSound = SoundID.Tink with { Pitch = 0.3f, PitchVariance = 0.25f };
            base.DustType = 291;
            AddMapEntry(new Color(255, 200, 0), CreateMapEntryName());
            MineResist = 0.01f;
            MinPick = 250;
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
            Projectile.NewProjectile(new EntitySource_TileBreak(i, j, null), new Vector2((float)(i + 1), (float)(j + 1)) * 16f, Vector2.Zero, ModContent.ProjectileType<AuricBoulderProj>(), 260, 0f, -1, 0f, 0f, 0f);
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
            r = 0.24f;
            g = 0.40f;
            b = 0.47f;
        }
        public override IEnumerable<Item> GetItemDrops(int i, int j) {
            yield return new Item(0, 1, 0);
            yield break;
        }
    }
}
