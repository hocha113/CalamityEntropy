using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.AncientPaladinWhip
{
    public class AncientPaladinWhipPlayer : ModPlayer
    {
        //召锤冷却(帧),每秒最多一把
        public const int HammerCooldown = 60;
        //剩余冷却(帧),只在本人客户端上有意义
        private int cooldownLeft;

        public override void PostUpdate() {
            if (cooldownLeft > 0)
                cooldownLeft--;
        }

        public bool TryUseHammer() {
            if (cooldownLeft > 0)
                return false;
            cooldownLeft = HammerCooldown;
            return true;
        }
    }
}
