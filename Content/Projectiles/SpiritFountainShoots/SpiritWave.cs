using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SpiritFountainShoots
{
    public class SpiritWave : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }
        public override void SetDefaults() {
            Projectile.width = 128;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void AI() {
            Projectile.Opacity = CEUtils.Parabola(Projectile.timeLeft / 60f, 1);
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + new Vector2(0, -6000), targetHitbox, (int)(60 * Projectile.Opacity));
        }

        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> points = new();
            for (float i = 0; i <= 1; i += 0.002f) {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + new Vector2(0, -6000), i));
            }
            Texture2D tx = CEUtils.getExtraTex("KingGrandDeathRay2");

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();

                float w = 70;
                float p = -Main.GlobalTimeWrappedHourly * 12;
                for (int i = 1; i < points.Count; i++) {
                    float wd = (1f + 0.08f * (float)Math.Cos(i * 0.06f + Main.GlobalTimeWrappedHourly * -16)) * Projectile.Opacity;
                    Color b = new Color(220, 220, 255) * wd;
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * Projectile.scale * w * wd,
                          new Vector3(i * 0.04f + Main.GlobalTimeWrappedHourly * -12, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * Projectile.scale * w * wd,
                          new Vector3(i * 0.04f + Main.GlobalTimeWrappedHourly * -12, 0, 1),
                          b));
                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

}