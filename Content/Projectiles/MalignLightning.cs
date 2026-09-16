using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class MalignLightning : ModProjectile
    {
        List<Vector2> points = new List<Vector2>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 128;

        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void AI() {
            if (Projectile.ai[0] == 0) {
                Vector2 vc;
                vc = Projectile.Center;

                Vector2 avc = Projectile.velocity;
                avc.Normalize();
                for (int i = 0; i < 22; i++) {
                    points.Add(vc);
                    vc += avc * 36;
                    avc = avc.RotatedByRandom(0.42f);
                }
            }
            Projectile.ai[0] += 1;
            if (Projectile.ai[0] > 10) {
                Projectile.Kill();
            }
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (points.Count < 1) {
                return false;
            }
            for (int i = 1; i < points.Count; i++) {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 4)) {
                    return true;
                }
            }
            return false;
        }
        public float PrimitiveWidthFunction(float completionRatio, Vector2 vertex) => Projectile.scale * 8 * ((10f - Projectile.ai[0]) / 10f);

        public Color PrimitiveColorFunction(float completionRatio, Vector2 vertex) {
            Color color = new Color(255, 100, 255);
            return color;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (points.Count < 1) {
                return false;
            }
            Color color = new Color(255, 200, 255);
            CEUtils.DrawLines(points, color, 8 * (1 - (Projectile.ai[0] / 10f)));
            CEUtils.DrawLines(points, color * 0.4f, 16 * (1 - (Projectile.ai[0] / 10f)));
            return false;
        }
    }

}