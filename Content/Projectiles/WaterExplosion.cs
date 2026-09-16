using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class WaterExplosion : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults() {
            // 原盗贼职业并入原版：炸弹类归远程
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
                //PRT_DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, Color.AliceBlue, 0.1f).Configure(new Vector2(2f, 2f), 0, 2 * 0.85f, 46);  //DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, Color.SkyBlue, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 2.2f * 0.65f, 30);
                if (Main.myPlayer == Projectile.owner) {
                    for (int i = 0; i < 32; i++) {
                        // 原灾厄 WaterShot 水弹改用原版水流弹
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6, 18), ProjectileID.WaterStream, Projectile.damage / 10, Projectile.knockBack + 0.1f, Projectile.owner);
                    }
                }
            }
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }

}