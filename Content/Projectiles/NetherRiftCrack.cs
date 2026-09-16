using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class NetherRiftCrack : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 360;
            Projectile.height = 360;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 56;
        }
        public override void AI() {
            //原盗贼职业判定改魔法: 两个发射源里 CrossBorderPursuit 已裁定为魔法, 其裂隙保持 1.4 倍体积
            if (Projectile.DamageType.CountsAsClass(DamageClass.Magic))
                Projectile.scale = 1.4f;
            if (Projectile.ai[0] == 0) {
                Projectile.rotation = CEUtils.randomRot();
            }
            Projectile.Opacity = Projectile.timeLeft / 60f;
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D t = CEUtils.getExtraTex("Cracks");
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, new Color(200, 200, 255) * Projectile.Opacity, Projectile.rotation, t.Size() / 2f, 3.6f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.Center.getRectCentered(280 * Projectile.scale, 280 * Projectile.scale).Intersects(targetHitbox);
        }
    }

}