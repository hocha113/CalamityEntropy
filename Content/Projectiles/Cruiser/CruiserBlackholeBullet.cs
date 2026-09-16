using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class CruiserBlackholeBullet : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;

        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
        }
        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 1000;
            Projectile.extraUpdates = 1;
        }
        public float ap = 0;
        public override void AI() {
            //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
            var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, Vector2.Zero, Color.White, 1f);
            p.Opacity = 0.5f;  //Opacity旧初始化器字段,Configure管不了
            Projectile.rotation = (new Vector2(Projectile.ai[1], Projectile.ai[2]) - Projectile.position).ToRotation();
            Projectile.velocity += Projectile.rotation.ToRotationVector2() * 0.08f;
            if (CEUtils.getDistance(new Vector2(Projectile.ai[1], Projectile.ai[2]), Projectile.Center) < Projectile.velocity.Length() + 20) {
                Projectile.Kill();
            }
            if (ap < 1) {
                ap += 0.01f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, Projectile.Center, new Vector2(Projectile.ai[1], Projectile.ai[2]), Color.Purple * ap * 0.45f, 5 * ap);
            return false;
        }
    }

}