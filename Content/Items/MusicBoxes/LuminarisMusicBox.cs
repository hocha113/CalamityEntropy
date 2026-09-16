using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.MusicBoxes
{
    public class LuminarisMusicBox : MusicBox
    {
        public override string MusicFile => "Assets/Sounds/Music/LuminarisBoss";
        public override int MusicBoxTile => ModContent.TileType<LuminarisMusicBoxTile>();
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ModContent.RarityType<Lunarblight>();
        }
    }
    public class LuminarisMusicBoxTile : MusicBoxTile
    {
    }
}
