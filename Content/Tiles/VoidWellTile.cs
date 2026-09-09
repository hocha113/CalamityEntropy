using System.Collections.Generic;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    public class VoidWellTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            RegisterItemDrop(ModContent.ItemType<VoidWell>());
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.Origin = new Point16(2, 2);
            TileObjectData.newTile.AnchorBottom = new AnchorData(
                AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide
                , TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 4, 0);
            TileObjectData.addTile(Type);
            Main.tileFrameImportant[(int)base.Type] = true;
            AddMapEntry(new Color(80, 0, 180), CEUtils.GetItemName<VoidWell>());

            DustType = DustID.CorruptTorch;

            List<int> adj = new List<int>
            {
                TileID.WorkBenches,
                TileID.Chairs,
                TileID.Tables,
                TileID.Anvils,
                TileID.MythrilAnvil,
                TileID.Furnaces,
                TileID.Hellforge,
                TileID.AdamantiteForge,
                TileID.TinkerersWorkbench,
                TileID.LunarCraftingStation,
                TileID.DemonAltar
            };
            if (CERef.Has && CEID.Tile_CosmicAnvil > 0)
            {
                adj.Add(CEID.Tile_CosmicAnvil);
            }
            AdjTiles = adj.ToArray();
        }
    }
}
