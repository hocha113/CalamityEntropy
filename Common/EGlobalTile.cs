using CalamityEntropy.Content.Items.Pets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class EGlobalTile : GlobalTile
    {
        /*public override bool CanExplode(int i, int j, int type)
        {
            return !SubworldSystem.IsActive<DimDungeonSubworld>();
        }
        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            return !SubworldSystem.IsActive<DimDungeonSubworld>();
        }
        public override bool CanPlace(int i, int j, int type)
        {
            return !SubworldSystem.IsActive<DimDungeonSubworld>();
        }*/
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem) {
            if (type == TileID.CorruptThorns || type == TileID.CrimsonThorns) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    if (Main.rand.NextBool(2000)) {
                        Item.NewItem(Item.GetSource_NaturalSpawn(), new Rectangle(i * 16, j * 16, 16, 16), new Item(ModContent.ItemType<VenomPiece>()));
                    }
                }
            }
        }

    }
}
