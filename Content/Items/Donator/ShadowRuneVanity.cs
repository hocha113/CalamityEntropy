using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class ShadowRuneVanity : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 34;
        }

        public override void AI() {
            if (Projectile.GetOwner().Entropy().hasAccVisual("ShadowRune")) {
                Projectile.timeLeft = 3;
            }
            else {
                Projectile.Kill();
            }
            Projectile.Center += ((Projectile.GetOwner().Center + new Vector2(-40 * Projectile.GetOwner().direction, -20)) - Projectile.Center) * 0.1f;
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, this.getTextureGlow()));
            return false;
        }

    }
}