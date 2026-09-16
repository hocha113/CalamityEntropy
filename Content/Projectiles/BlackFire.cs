using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class BlackFire : ModProjectile
    {
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 2400;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 30;
            Projectile.MaxUpdates = 9;
        }

        public override void AI() {
            if (Projectile.ai[0] == 0) {
                Projectile.velocity *= 0.14f;
            }
            Projectile.ai[0] += 0.1f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (int i = 0; i < 5; i++) {
                odp.Add(Projectile.Center + Projectile.velocity / 4f * i);
                odr.Add(Projectile.rotation);
                if (odp.Count > 360) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
            }

            NPC target = Projectile.FindTargetWithinRange(1100, false);
            if (target != null && Projectile.ai[0] > 8) {
                Projectile.velocity *= 0.98f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();

                Projectile.velocity += v * 0.06f;
            }


        }
        float trailOffset = 0;

        public override bool PreDraw(ref Color lightColor) {
            Color cl = Color.Lerp(Color.Black, Color.White, Projectile.ai[0] / 30f);
            float c = 0;
            trailOffset += 0.04f;

            c = 0;
            if (odp.Count > 1) {
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.Black;

                for (int i = 1; i < odp.Count; i++) {
                    float width = 0;
                    if (i > 270) {
                        float x = (float)(i - 270) / 90f;
                        if (1 - x * x < 0) {
                            width = 0;
                        }
                        else {
                            width = (float)Math.Sqrt(1 - x * x);
                        }
                    }
                    else {
                        width = 1f - ((float)(270 - i) / 270f);
                    }

                    c += 1f / odp.Count;
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + new Vector2(40 * width, 0).RotatedBy(odr[i] + MathHelper.PiOver2),
                          new Vector3((float)i / ((float)odp.Count) + trailOffset, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + new Vector2(-40 * width, 0).RotatedBy(odr[i] + MathHelper.PiOver2),
                          new Vector3((float)i / ((float)odp.Count) + trailOffset, 0, 1),
                          b));

                }
                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 4) {
                    Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }


                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
            return false;
        }

    }

}