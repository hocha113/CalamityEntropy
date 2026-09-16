using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core
{
    /// <summary>PortsDoT 玩家侧状态：目前只承载寒宇冻结（CosmicFreeze）。</summary>
    public class CEDoTPlayer : ModPlayer
    {
        /// <summary>寒宇冻结：站定不动时负生命回复减半</summary>
        public bool cosmicFreeze;

        public override void ResetEffects() {
            cosmicFreeze = false;
        }

        public override void UpdateBadLifeRegen() {
            bool standingStill = Player.velocity.X == 0f && Player.velocity.Y == 0f
                && Player.itemAnimation == 0;
            if (cosmicFreeze && standingStill && Player.lifeRegen < 0)
                Player.lifeRegen /= 2;
        }
    }
}
