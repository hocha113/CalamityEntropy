using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AbyssalCrack : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 128;
            Projectile.timeLeft = 48;
        }
        public List<Vector2> points = new List<Vector2>();
        int d = 0;
        public override void AI() {
            d++;
            if (d > 16) {
                Projectile.velocity *= 0;
            }
            else {
                Vector2 o = (points.Count > 0 ? points[points.Count - 1] : Projectile.Center - Projectile.velocity);
                Vector2 nv = Projectile.Center + CEUtils.randomVec(4);
                for (float i = 0.1f; i <= 1; i += 0.1f) {
                    points.Add(Vector2.Lerp(o, nv, i));
                }
            }
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (points.Count < 1) {
                return false;
            }
            for (int i = 1; i < points.Count; i++) {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 30)) {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {

            return false;
        }
        public void draw() {
            if (points.Count < 1) {
                return;
            }
            Texture2D px = CEExtraAssets.white;
            float jd = 1;
            float lw = Projectile.timeLeft / 30f;
            Color color = Color.White;
            for (int i = 1; i < points.Count; i++) {
                Vector2 jv = Vector2.Zero;
                CEUtils.drawLine(Main.spriteBatch, px, points[i - 1], points[i] + jv, color * jd, 1f * lw * (new Vector2(-30, 0).RotatedBy(MathHelper.ToRadians(180 * ((float)i / points.Count)))).Y, 3);
            }
        }
    }

}