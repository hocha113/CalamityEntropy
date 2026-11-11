using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    /// <summary>沙面探测,只读已同步的物块,各端结果一致</summary>
    internal static class SandseaTerrain
    {
        /// <summary>实心物块,不含平台</summary>
        public static bool Solid(int i, int j) {
            if (!WorldGen.InWorld(i, j, 1)) {
                return false;
            }
            Tile tile = Main.tile[i, j];
            return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        /// <summary>实心物块或平台</summary>
        public static bool Standable(int i, int j) {
            if (!WorldGen.InWorld(i, j, 1)) {
                return false;
            }
            Tile tile = Main.tile[i, j];
            return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType];
        }

        public static bool Buried(Vector2 worldPos) => Solid((int)(worldPos.X / 16f), (int)(worldPos.Y / 16f));

        /// <summary>
        /// (x, startY) 处的沙面世界 Y:起点埋在物块里就向上找出口,否则向下找落点
        /// 找不到时返回探测范围的尽头
        /// </summary>
        /// <param name="platforms">向下找落点时平台是否算数</param>
        public static float Surface(float x, float startY, bool platforms = false) {
            int i = Math.Clamp((int)(x / 16f), 2, Main.maxTilesX - 3);
            int j = Math.Clamp((int)(startY / 16f), 2, Main.maxTilesY - 3);
            int limit = SandseaDirector.SurfaceProbeTiles;
            if (Solid(i, j)) {
                for (int k = 0; k < limit && j > 2; k++) {
                    if (!Solid(i, j - 1)) {
                        break;
                    }
                    j--;
                }
                return j * 16f;
            }
            for (int k = 0; k < limit && j < Main.maxTilesY - 3; k++) {
                j++;
                if (platforms ? Standable(i, j) : Solid(i, j)) {
                    break;
                }
            }
            return j * 16f;
        }
    }
}
