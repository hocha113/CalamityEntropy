using CalamityEntropy.Content.Buffs.PortsDoT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopBlackProjectile : PoopProj
    {
        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
            if (Projectile.owner == Main.myPlayer) {
                CalamityEntropy.blackMaskTime = 4 * 60;
            }
            if (Projectile.owner == Main.myPlayer) {
                foreach (NPC npc in Main.ActiveNPCs) {
                    if (CEUtils.getDistance(npc.Center, Projectile.Center) < 4000) {
                        NPC.HitInfo hit = npc.CalculateHitInfo(Projectile.damage / 8, 0, false, 0, Projectile.DamageType);
                        npc.StrikeNPC(hit);
                        npc.AddBuff(31, 4 * 60);
                        npc.AddBuff(ModContent.BuffType<TemporalSadness>(), 4 * 60);
                    }
                }
            }
        }
    }

}