using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VoidBurst : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 640;
            Projectile.height = 640;
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
                for (float i = 0; i <= 1; i += 0.2f) {
                    //CustomPulse贴图路径现传,走PRTPathTextures缓存,Configure第一个string是TexPath
                    PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Lerp(Color.Blue, Color.BlueViolet, i) * 0.9f, 0.01f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.01f, i * 0.4f, 8 + (int)(i * 30));  //LargeBloom/FlameExplosion那些Calamity路径字符串原样保留
                }
                //DetailedExplosionCal收尾,Configure三参Calamity explode原样
                PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, new Color(180, 156, 255) * 0.8f, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 2.2f, 25);
            }
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 80, 2, 600, 18);
        }
    }

}