using CalamityEntropy.Utilities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Tail : ModProjectile
    {

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 3;
            Projectile.width = 2;
            Projectile.height = 2;
        }

        public override void AI() {
            Projectile.timeLeft = 3;
            var player = Projectile.owner.ToPlayer();
            Projectile.Center = player.MountedCenter + Vector2.UnitY * player.gfxOffY + new Vector2(-8 * player.direction, 4).RotatedBy(player.fullRotation);
            if (rope == null) {
                rope = new Rope(Projectile.Center, 6, 8, new Vector2(0, 0.26f), 0.25f, 26, true);
            }
            rope.Start = Projectile.Center - player.velocity / 4 * 3;
            rope.Update();
            rope.Start = Projectile.Center - player.velocity / 4 * 2;
            rope.Update();
            rope.Start = Projectile.Center - player.velocity / 4 * 1;
            rope.Update();
            rope.Start = Projectile.Center;
            rope.Update();
            var points = rope.GetPoints();
            odp.Clear();
            odp.Add(points[0]);
            for (int i = 1; i < points.Count; i++) {
                for (float j = 0.25f; j <= 1f; j += 0.25f) {
                    odp.Add(Vector2.Lerp(points[i - 1], points[i], j));
                }

            }
        }
        public Rope rope = null;
        public List<Vector2> odp = new List<Vector2>();
        public override bool PreDraw(ref Color lightColor) {
            Color cl = Color.Lerp(Color.Black, Color.White, Projectile.ai[0] / 30f);
            float c = 0;


            c = 0;
            if (odp.Count > 1) {
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 6,
                          new Vector3((float)0, 1, 1),
                          b));
                ve.Add(new ColoredVertex(odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 6,
                      new Vector3((float)0, 0, 1),
                      b));
                for (int i = 1; i < odp.Count; i++) {


                    c += 1f / odp.Count;
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 6,
                          new Vector3((float)(i + 1) / odp.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 6,
                          new Vector3((float)(i + 1) / odp.Count, 0, 1),
                          b));

                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
            return false;
        }

    }

}