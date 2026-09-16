using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Utilities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Theostring : ModProjectile
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
            Projectile.timeLeft = 10;
            Projectile.netImportant = true;
        }

        public override void AI() {
            if (Projectile.GetOwner().dead) {
                Projectile.Kill();
                return;
            }
            if (!Projectile.GetOwner().GetModPlayer<VanityModPlayer>().TheocracyMark) {
                return;
            }
            Projectile.timeLeft = 5;
            var player = Projectile.owner.ToPlayer();
            Projectile.Center = player.MountedCenter + Vector2.UnitY * player.gfxOffY + new Vector2(-26 * Projectile.ai[0], -16).RotatedBy(player.fullRotation + player.headRotation) - player.velocity;
            if (rope == null) {
                rope = new Rope(Projectile.Center, 6, 5, new Vector2(0, 0.1f), 0.2f, 30, false);
            }
            rope.gravity = new Vector2(Main.windSpeedCurrent * 0.16f * (0.4f + 0.6f * (float)(Math.Cos(Main.GameUpdateCount * 0.1f) + 1) * 0.5f), 0.1f);
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
        public override bool? CanDamage() {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            var gdv = Main.graphics.GraphicsDevice;

            Color cl = Color.Lerp(Color.Black, Color.White, Projectile.ai[0] / 30f);
            float c = 0;


            c = 0;

            Vector2 calP(Vector2 org, float zoom) {
                Vector2 scrs = Main.ScreenSize.ToVector2() / 2f;
                return scrs + (org - scrs) / zoom;
            }
            if (odp.Count > 1) {
                Main.spriteBatch.End();
                EffectLoader.PreparePixelShader(gdv);
                int xp = Projectile.GetOwner().direction * -4 - 2;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
                b = b * Projectile.GetOwner().Entropy().alpha;
                ve.Add(new ColoredVertex(calP(new Vector2(xp, 0) + odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 3, Main.GameViewMatrix.Zoom.X),
                          new Vector3((float)0, 1, 1),
                          b));
                ve.Add(new ColoredVertex(calP(new Vector2(xp, 0) + odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 3, Main.GameViewMatrix.Zoom.X),
                      new Vector3((float)0, 0, 1),
                      b));
                for (int i = 1; i < odp.Count; i++) {
                    c += 1f / odp.Count;
                    ve.Add(new ColoredVertex(calP(new Vector2(xp, 0) + odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 3, Main.GameViewMatrix.Zoom.X),
                          new Vector3((float)(i + 1) / odp.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(calP(new Vector2(xp, 0) + odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 3, Main.GameViewMatrix.Zoom.X),
                          new Vector3((float)(i + 1) / odp.Count, 0, 1),
                          b));

                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = gdv;
                if (ve.Count >= 3) {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

                    Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                    Main.spriteBatch.End();
                    EffectLoader.ApplyPixelShader(gdv, Projectile.GetOwner().GetModPlayer<VanityModPlayer>().TheocrazyDyeItemID, Projectile.GetOwner(), true);
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                }
            }

            return false;
        }

    }

}