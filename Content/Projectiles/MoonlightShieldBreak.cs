using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class MoonlightShieldBreak : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 640;
            Projectile.height = 640;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 200;

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.ai[0] < 2;
        }
        public override void AI() {
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 1) {
                Projectile.ai[2] = 1;
                foreach (Projectile p in Main.projectile) {
                    if (p.active && p.getRect().Intersects(Projectile.getRect())) {
                        if (p.hostile && (p.ModProjectile == null || p.ModProjectile.ShouldUpdatePosition())) {
                            p.velocity += (p.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * p.velocity.Length();
                            p.hostile = false;
                            p.friendly = true;
                        }
                    }
                }
            }
            Projectile.ai[1] += addSize;
            addSize *= 0.94f;
            if (Projectile.ai[0] > 4) {
                Projectile.ai[2] *= 0.8f;
            }
        }
        public float addSize = 0.5f;
        public override bool PreDraw(ref Color lightColor) {

            return false;
        }
    }

}