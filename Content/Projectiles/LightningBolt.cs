using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static CalamityEntropy.CEUtils;

namespace CalamityEntropy.Content.Projectiles
{
    public class LightningBolt : ModProjectile
    {
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 12;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 80;
            Projectile.extraUpdates = 1;
        }

        public override void AI() {
            Vector2 ps = Projectile.velocity + Projectile.Center + CEUtils.randomPointInCircle(42);
            for (float i = 0.1f; i <= 1; i += 0.1f) {
                oldPos.Add(Vector2.Lerp(Projectile.Center, ps, i));
                if (oldPos.Count > 200) {
                    oldPos.RemoveAt(0);
                }
            }
            Lighting.AddLight(ps, 1, 1, 1);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(oldPos[0], Projectile.Center, targetHitbox, (int)(20 * Projectile.scale));
        }

        public override bool PreDraw(ref Color lightColor) {
            if (oldPos.Count < 3)
                return false;
            List<VertexPointSets> v = new List<VertexPointSets>();
            var gd = Main.graphics.GraphicsDevice;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < oldPos.Count; i++) {
                float a = (i / (oldPos.Count - 1f));
                float w = CEUtils.Parabola(a, 1);
                v.Add(new VertexPointSets(oldPos[i], Color.Aqua, 36 * w, (i / (oldPos.Count - 1f)) * 3f + Main.GlobalTimeWrappedHourly * 4));
            }
            var ve = v.GetVertexesList(false);
            gd.Textures[0] = CEExtraAssets.VoltTrailThicc;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            v.Clear();
            for (int i = 0; i < oldPos.Count; i++) {
                float a = (i / (oldPos.Count - 1f));
                float w = CEUtils.Parabola(a, 1);
                v.Add(new VertexPointSets(oldPos[i], Color.White, 16 * w, (i / (oldPos.Count - 1f)) * 3f + Main.GlobalTimeWrappedHourly * 6));
            }
            ve = v.GetVertexesList(false);
            gd.Textures[0] = CEExtraAssets.VoltTrailThicc;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }


}