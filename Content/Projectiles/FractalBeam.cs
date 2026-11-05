using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FractalBeam : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public const float RayLength = 1200f;
        public override bool ShouldUpdatePosition() => false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.normalize() * RayLength, targetHitbox, 16);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity.normalize() * RayLength, 16, DelegateMethods.CutTiles);
        }
        public override void AI() {
            if (Projectile.localAI[0]++ != 0 || Main.dedServ)
                return;
            //初速递增,衰减后铺满射线
            Vector2 dir = Projectile.velocity.normalize();
            for (int j = 0; j < 120; j += 2) {
                PRTLoader.NewParticle<PRT_HeavenfallStar>(Projectile.Center, dir * (0.1f + j * 0.34f) * 3, Color.SkyBlue, Main.rand.NextFloat(0.6f, 1.3f))
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, dir.ToRotation(), 24);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }
}
