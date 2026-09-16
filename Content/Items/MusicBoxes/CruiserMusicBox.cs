using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.MusicBoxes
{
    public class CruiserMusicBox : MusicBox
    {
        public override string MusicFile => "Assets/Sounds/Music/CruiserBoss";
        public override int MusicBoxTile => ModContent.TileType<CruiserMusicBoxTile>();
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }
    public class CruiserMusicBoxTile : MusicBoxTile
    {
    }
}
