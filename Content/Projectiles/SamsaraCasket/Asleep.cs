using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class Asleep : SamsaraSword
    {
        public float counter { get { return Projectile.ai[0]; } set { Projectile.ai[0] = value; } }
        public float rot = 0;

        public override void attackAI(NPC t) {
            if (counter == 0 || counter == 20 || counter == 40) {
                Vector2 tPos = t.Center + (t.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * (120 + (t.width + t.height) / 2);
                Projectile.velocity = (tPos - Projectile.Center) / 20;
            }
            if (counter >= 60) {
                Projectile.velocity *= 0.96f;
                Projectile.velocity += ((t.Center + (Projectile.Center - t.Center).SafeNormalize(Vector2.Zero) * (140 + (t.width + t.height) / 2)) - Projectile.Center).SafeNormalize(Vector2.Zero) * 1f;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (t.Center - Projectile.Center).ToRotation(), 0.2f, false);

            }
            else {
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.4f, false);
            }
            if (counter == 88 && Main.myPlayer == Projectile.owner) {
                SoundEngine.PlaySound(SoundID.Item122, Projectile.position);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, ((t.Center - Projectile.Center).SafeNormalize(Vector2.One) * 24).RotatedBy(MathHelper.ToRadians(0)), ModContent.ProjectileType<AsleepPhantom>(), Projectile.damage * 2, Projectile.knockBack, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, ((t.Center - Projectile.Center).SafeNormalize(Vector2.One) * 24).RotatedBy(MathHelper.ToRadians(16)), ModContent.ProjectileType<AsleepPhantom>(), Projectile.damage * 2, Projectile.knockBack, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, ((t.Center - Projectile.Center).SafeNormalize(Vector2.One) * 24).RotatedBy(MathHelper.ToRadians(-16)), ModContent.ProjectileType<AsleepPhantom>(), Projectile.damage * 2, Projectile.knockBack, Projectile.owner);

            }
            counter++;
            if (counter >= 120) {
                counter = 0;
            }

            if (counter < 60) {
                setDamage(1.4f);
            }
        }
    }
}
