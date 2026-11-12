using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.NoontideRapier
{
    //七彩矢冷却放在玩家身上,松开再按也不会刷新;只在本人客户端上有意义
    public class NoontideRapierPlayer : ModPlayer
    {
        public const int BoltInterval = 20;   //帧,策划 0.33 秒一发,不吃攻速

        private int boltCooldown;

        public override void PostUpdate() {
            if (boltCooldown > 0)
                boltCooldown--;
        }

        public bool TryFireBolt() {
            if (boltCooldown > 0)
                return false;
            boltCooldown = BoltInterval;
            return true;
        }
    }
}
