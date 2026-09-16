using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content
{
    public class NMSGItem : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public bool IsShootCountCorlUse;
        public override bool CanUseItem(Item item, Player player) {
            if (player.TFAW().DontUseItemTime > 0) {
                return false;
            }
            return IsShootCountCorlUse
                ? player.ownedProjectileCounts[item.shoot] <= 0 : base.CanUseItem(item, player);
        }
    }
}
