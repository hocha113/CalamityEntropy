using CalamityEntropy.Content.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LMSpawner : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 10;
        }

        public override void AI() {
            if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer) {
                foreach (NPC nc in Main.npc) {
                    if (nc.type == ModContent.NPCType<LotteryMachine>()) {
                        nc.active = false;

                    }

                }
                int n = NPC.NewNPC(Main.player[0].GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y, ModContent.NPCType<LotteryMachine>());
                Main.npc[n].Center = Projectile.Center + new Vector2(0, -60);
            }
            Projectile.Kill();
        }


    }

}