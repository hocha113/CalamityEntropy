using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    //脱离灾厄:灾厄AquashardSplit同短名移植(水刃大招分裂弹),运动与判定逐式对齐,贴图复用自有主弹
    public class AquashardSplit : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Projectiles/AquashardThrow";

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
        }

        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft < 280 && target.CanBeChasedBy(Projectile);

        public override void AI() {
            Projectile.velocity.X *= 0.9995f;
            Projectile.velocity.Y += 0.01f;

            if (Projectile.timeLeft < 280) {
                //原灾厄HomeInOnNPC(450,6/8,20),tileCollide=true时带视线过滤
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 450f,
                    Projectile.tileCollide ? CEUtils.HomingWithTileBlockingFilter : null);
                if (target != null)
                    Projectile.HomingNPCBetter(target, 450f, Projectile.ai[1] == 1f ? 8f : 6f, 20f, giveExtraUpdate: 1, ignoreDist: true);
            }
        }

        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);
            for (int k = 0; k < 5; k++) {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Rain, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
            }
        }
    }
}
