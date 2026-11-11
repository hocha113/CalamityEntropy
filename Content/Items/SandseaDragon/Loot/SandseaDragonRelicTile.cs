using CalamityEntropy.Content.Tiles;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon.Loot
{
    public class SandseaDragonRelicTile : CEBaseBossRelic
    {
        public override string RelicTextureName => "CalamityEntropy/Content/Items/SandseaDragon/Loot/SandseaDragonRelicTile";
        public override int AssociatedItem => ModContent.ItemType<SandseaDragonRelic>();
    }
}
