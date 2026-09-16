using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopRainbowProjectile : PoopProj
    {
        public override void OnKill(int timeLeft) {
            foreach (Player player in Main.ActivePlayers) {
                player.Heal(160);
            }
            if (!Main.dedServ) {
                CEUtils.PlaySound("happy rainbow with giggle", 1);
            }
        }
        public override int dustType => DustID.RainbowMk2;
    }

}