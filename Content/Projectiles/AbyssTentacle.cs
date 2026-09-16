using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AbyssTentacle : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 256;
            Projectile.height = 256;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 2f;
            Projectile.timeLeft = 80;
            Projectile.penetrate = -1;
            Projectile.ArmorPenetration = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }
        float scale = 0;
        float scalej = 0.25f;
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            scale += scalej;
            scalej -= 0.03f;
            if (scale < 0) {
                Projectile.Kill();
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 200 * scale, targetHitbox, 32);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D t1 = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(t1, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(0, t1.Height / 2), scale, SpriteEffects.None, 0);

            return false;
        }


    }


}