using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class SpiritBannerMinion : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 4;
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 36;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.minion = true;
            Projectile.minionSlots = 3;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 300);
        }
        public override void AI() {
            Player player = Main.player[Projectile.owner];
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5) {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 3) {
                    Projectile.frame = 0;
                }
            }
            Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY + new Vector2(-10 * player.direction, -18);
            if (player.HasBuff(ModContent.BuffType<SpiritGathering>())) {
                Projectile.timeLeft = 3;
            }
            Projectile.ai[0]--;
            if (Projectile.owner == Main.myPlayer) {
                NPC target = CEUtils.findTarget(player, Projectile, 4600);
                if (target != null) {
                    if (Projectile.ai[0] <= -20) {
                        Projectile.ai[0] = 80;
                        CEUtils.PlaySound("soulScreem", 1, Projectile.Center, volume: 0.28f);
                        for (int i = 0; i < 3; i++) {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(0, -16), new Vector2(0, -12).RotateRandom(1.2f), ModContent.ProjectileType<SpiritLightSoul>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                    }
                }
            }
        }

    }
}

