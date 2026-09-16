using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class AstralExplosion : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 400;
            Projectile.height = 400;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 126;
        }
        public override void AI() {
            if (Projectile.ai[0] == 0) {
                CEUtils.PlaySound("explosion", 1, Projectile.Center, 4);
                //PRT_DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, new Color(160, 120, 255), 0.1f).Configure(new Vector2(2f, 2f), 0, 2 * 0.85f, 46);  //DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, new Color(180, 156, 255), 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 2.2f * 0.65f, 30);
            }
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }

}