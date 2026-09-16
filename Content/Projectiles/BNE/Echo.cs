using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.BNE
{
    public class Echo : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 260;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 80;
            Projectile.light = 1;
            Projectile.MaxUpdates = 4;
        }
        public override void AI() {
            //PRT_EchoCircle rotation走Configure尾参,旧构造最后一个rotation参数
            PRTLoader.NewParticle<PRT_EchoCircle>(Projectile.Center, Projectile.velocity, Color.White, 1f)
                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation());  //AdditiveBlend+rotation都在Configure,跟EParticle尾参顺序不同
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }


    }

}