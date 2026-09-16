using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class Dirge : SamsaraSword
    {
        public override void AI() {
            base.AI();

        }
        public override void attackAI(NPC t) {
            Vector2 targetPos = t.Center + new Vector2(0, 100 + t.height);
            Projectile.velocity = (targetPos - Projectile.Center) * 0.1f;
            Projectile.rotation = (t.Center - Projectile.Center).ToRotation();
            setDamage(2);
            if (Projectile.ai[1]++ % 40 == 0) {
                SoundEngine.PlaySound(SoundID.Item88, Projectile.position);
                if (Main.myPlayer == Projectile.owner) {
                    for (int i = 0; i < 4; i++) {
                        float rot = CEUtils.randomRot();
                        int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), t.Center + rot.ToRotationVector2() * 400, rot.ToRotationVector2() * -16, 645, Projectile.damage, Projectile.knockBack, Projectile.owner);
                        p.ToProj().DamageType = Projectile.DamageType;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            lightColor = Color.White;
            return base.PreDraw(ref lightColor);
        }
    }
}
