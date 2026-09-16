using CalamityEntropy.Content.Items;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Tiles
{
    public class VoidDestroyerRelicTile : CEBaseBossRelic
    {
        public override string RelicTextureName => "CalamityEntropy/Content/Tiles/VoidDestroyerRelicTile";
        public override int AssociatedItem => ModContent.ItemType<VoidDestroyerRelic>();
    }
}
