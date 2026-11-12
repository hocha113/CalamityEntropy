using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarBlessing
{
    //星团的冷却;只有本人客户端会去扣,其他端上的值不作数
    public class StarBlessingPlayer : ModPlayer
    {
        //0.75秒(策划)
        public const int ClusterCooldown = 45;

        private int cooldownLeft;

        public override void PostUpdate() {
            if (cooldownLeft > 0)
                cooldownLeft--;
        }

        public bool TryRelease() {
            if (cooldownLeft > 0)
                return false;
            cooldownLeft = ClusterCooldown;
            return true;
        }
    }
}
