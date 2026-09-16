using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Projectiles
{
    public class BrimstoneBarrageFriendly : ModProjectile
    {
        public int time = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 18;
            Projectile.height = 44;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 690;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI() {

            if (Projectile.velocity.Length() < Projectile.ai[2]) {
                Projectile.velocity *= 1.01f;
                if (Projectile.velocity.Length() > Projectile.ai[2]) {
                    Projectile.velocity.Normalize();
                    Projectile.velocity *= Projectile.ai[2];
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;

            if (Projectile.timeLeft < 60)
                Projectile.Opacity = MathHelper.Clamp(Projectile.timeLeft / 60f, 0f, 1f);

            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
            }
            Lighting.AddLight(Projectile.Center, 0.75f * Projectile.Opacity, 0f, 0f);
            time++;
        }


        public override bool PreDraw(ref Color lightColor) {
            lightColor.R = (byte)(255 * Projectile.Opacity);

            CEUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CEUtils.CircularHitboxCollision(Projectile.Center, 18 * Projectile.scale, targetHitbox);
    }
}
