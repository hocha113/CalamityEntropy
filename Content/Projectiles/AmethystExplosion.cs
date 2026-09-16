using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class AmethystExplosion : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 400;
            Projectile.height = 400;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0.6f;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 126;
        }
        public override void AI() {
            if (Projectile.ai[0] == 0) {
                //DetailedExplosionCal Configure三参是Calamity explode原样,别套EParticle尾参
                PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, new Color(220, 196, 255), 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 2.2f * 0.65f, 30);
                float scale = 7f;
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.MediumPurple * 1.6f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 30);
                //CustomPulse贴图路径现传,走PRTPathTextures缓存,Configure第一个string是TexPath
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.MediumPurple * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 26);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.MediumPurple * 1.2f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 22);
            }
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }

}