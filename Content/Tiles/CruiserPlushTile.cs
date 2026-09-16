using CalamityEntropy.Content.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    public class CruiserPlushTile : ModTile
    {
        public override void SetStaticDefaults() {
            RegisterItemDrop(ModContent.ItemType<CruiserPlush>());
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 4, 0);
            TileObjectData.addTile(Type);
            Main.tileFrameImportant[(int)base.Type] = true;
            AddMapEntry(new Color(63, 72, 200));

            // 脱离灾厄:原用灾厄紫色宇宙尘,改原版紫炬光尘
            DustType = DustID.PurpleTorch;
        }

    }
}
