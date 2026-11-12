using CalamityEntropy.Content.Items.VoidSlime;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    public class VoidSlimeRelicTile : CEBaseBossRelic
    {
        public override string RelicTextureName => "CalamityEntropy/Content/Tiles/VoidSlimeRelicTile";
        public override int AssociatedItem => ModContent.ItemType<VoidSlimeRelic>();
    }

    public class VoidSlimeTrophyTile : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileSpelunker[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.addTile(Type);
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;
            AddMapEntry(new Color(120, 85, 60), Language.GetText("MapObject.Trophy"));
            DustType = 7;
        }
    }

    /// <summary>星河固化机,3x3 落地制作站,配方里等同远古操纵机、秘银砧(并当作砧)与固化机</summary>
    public class GalaxySolidifierTile : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.addTile(Type);
            TileID.Sets.DisableSmartCursor[Type] = true;
            AdjTiles = new int[] { TileID.Solidifier, TileID.LunarCraftingStation, TileID.MythrilAnvil, TileID.Anvils };
            AddMapEntry(new Color(116, 81, 178), CreateMapEntryName());
            DustType = DustID.PurpleTorch;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
            r = 0.3f;
            g = 0.16f;
            b = 0.48f;
        }
    }
}
