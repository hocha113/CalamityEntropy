using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class GiantBoulderProj : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.scale = 1f;
            Projectile.timeLeft = 1600 + 600;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 25;
            AIType = 1013;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI() {
            Projectile.rotation += Projectile.velocity.X * -0.045f;
            if (Math.Abs(Projectile.velocity.X) < 16)
                Projectile.velocity.X *= 1.016f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (Projectile.velocity.X != oldVelocity.X) {
                if (!Main.dedServ) {
                    ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(oldVelocity.normalize() * Vector2.UnitX * -6, oldVelocity.Length() * 0.4f), Projectile.Distance(Main.LocalPlayer.Center));
                    CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(0.6f, 0.75f), Projectile.Center, 16, 0.4f);
                }
                Projectile.velocity.X = -oldVelocity.X * 1f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y) {
                if (!Main.dedServ) {
                    ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(oldVelocity.normalize() * Vector2.UnitY * -6, oldVelocity.Length() * 0.4f), Projectile.Distance(Main.LocalPlayer.Center));
                    for (int i = 0; i < 8; i++)
                        Dust.NewDustDirect(Projectile.Center + new Vector2(Main.rand.NextFloat(-1, 1) * 80 * Projectile.scale, 80 * Projectile.scale), 0, 0, DustID.Stone).scale = 2;
                    if (Math.Abs(Projectile.velocity.Y) > 1 || Main.rand.NextBool(32))
                        CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(0.4f, 0.5f), Projectile.Center, 160, 0.4f);
                }
                Projectile.velocity.Y = -oldVelocity.Y * 0.5f;
            }
            if (Projectile.timeLeft < 1600)
                return true;
            return false;
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(0.4f, 0.5f), Projectile.Center, 160, 0.4f);
            for (int i = 0; i < 64; i++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Stone).scale = 2;
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                for (int i = 0; i < 16; i++) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + CEUtils.randomPointInCircle(140), CEUtils.randomPointInCircle(36), ProjectileID.Boulder, 80, 6);
                }
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.velocity += Projectile.velocity * 2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.velocity += Projectile.velocity * 2 * target.knockBackResist;
        }
    }
}
