using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VoidWave : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 32;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1 * 60 * 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 4;
            Projectile.tileCollide = false;
        }
        bool init = true;
        public float counter = 0;
        public float pg = 0;
        public bool playSound = true;
        public override void AI() {
            if (Projectile.timeLeft < 200) {
                Projectile.Opacity = Projectile.timeLeft / 200f;
            }
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 4000);
            if (target != null) {
                Projectile.SmoothHomingBehavior(target.Center, 1, 0.01f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            counter++;
        }
        public override bool PreDraw(ref Color lightColor) {
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++) {
                float prog = ((float)i / ProjectileID.Sets.TrailCacheLength[Type]);
                Color clr = Color.White * 0.4f * (1 - prog);
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, clr, Projectile.oldRot[i]);
            }
            Draw(Projectile.Center, Color.White * 0.8f, Projectile.rotation);
            return false;
        }
        public void Draw(Vector2 pos, Color lightColor, float rotation) {
            float rot = rotation;
            var tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, null, lightColor * Projectile.Opacity, rot, tex.Size() * 0.5f, Projectile.scale * 2, SpriteEffects.None);
        }
    }


}