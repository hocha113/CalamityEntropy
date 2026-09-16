using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class ExoShot1 : EBookBaseProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 4;

        }
        public bool s = true;
        public override void AI() {
            base.AI();
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 4 == 0) {
                Projectile.frame++;
                if (Projectile.frame > 3)
                    Projectile.frame = 0;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (s) {
                s = false;
                Projectile.scale *= 0.6f;
            }
        }
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.ignoreWater = true;
            Projectile.width = Projectile.height = 70;
            Projectile.timeLeft = 120;
            Projectile.penetrate = 3;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (s) {
                s = false;
                Projectile.scale *= 0.6f;
            }
            lightColor = Color.White;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
    public class ArMinionLaser : EBookBaseLaser
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public float w = 1f;
        public override bool PreAI() {
            if (w == 1f) {
                Projectile.penetrate += 4;
            }
            return base.PreAI();
        }
        public override void AI() {
            base.AI();
            w -= 0.1f;
            if (w <= 0) {
                Projectile.Kill();
            }
        }
        public override Color baseColor => new Color(255, 120, 67);

        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> points = this.getSamplePoints();
            if (points.Count < 2) {
                return false;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            {
                Texture2D tx = CEExtraAssets.MegaStreakBacking2b;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = this.color * w;
                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++) {
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 24 * Projectile.scale * w,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 24 * Projectile.scale * w,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width);
                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            {
                Texture2D tx = CEExtraAssets.MegaStreakInner;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(255, 246, 246) * w;
                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++) {
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18 * Projectile.scale * w,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18 * Projectile.scale * w,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width);
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            CEUtils.DrawGlow(points[points.Count - 1], color * w, 1.6f * Projectile.scale * w, false);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}