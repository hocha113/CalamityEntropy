using CalamityEntropy.Content.Tiles;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.MusicBoxes
{
    public class ProphetMusicBox : MusicBox
    {
        public override string MusicFile => "Assets/Sounds/Music/SpectralForesight";
        public override int MusicBoxTile => ModContent.TileType<ProphetMusicBoxTile>();
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Yellow;
        }
    }
    public class ProphetMusicBoxTile : MusicBoxTile
    {
    }
}
