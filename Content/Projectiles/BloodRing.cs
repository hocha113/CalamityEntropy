using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class BloodRing : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 80;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;
            Projectile.ArmorPenetration = 12800;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.DisableCrit();
        }
        public override void AI() {

            if (playSound) {
                playSound = false;
                CEUtils.PlaySound("maw of the void", 1, Projectile.Center, 8);
            }
            Projectile.Center = Projectile.owner.ToPlayer().Center + Projectile.owner.ToPlayer().gfxOffY * Vector2.UnitY;
        }
        public override void OnKill(int timeLeft) {
            Projectile.owner.ToPlayer().headRotation = 0f;
        }
        public List<Vector2> getPoints() {
            float dist = 200f - (float)Math.Cos(Projectile.timeLeft * 0.1f) * 32;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 60; i++) {
                points.Add(Projectile.Center + new Vector2(dist, 0).RotatedBy(MathHelper.ToRadians(i * 6 - 5 * Projectile.timeLeft)));
            }
            return points;
        }
        public List<Vector2> getPointsRelative(float distAdd = 0) {
            float dist = 200f - (float)Math.Cos(Projectile.timeLeft * 0.1f) * 32 + distAdd;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 60; i++) {
                points.Add(new Vector2(dist, 0).RotatedBy(MathHelper.ToRadians(i * 6 - 5 * Projectile.timeLeft)));
            }
            return points;
        }
        public bool playSound = true;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            int width = 64;
            List<Vector2> points = getPoints();
            for (int i = 0; i < points.Count; i++) {
                if (i == 0) {
                    if (CEUtils.LineThroughRect(points[0], points[points.Count - 1], targetHitbox, width)) {
                        return true;
                    }
                }
                else {
                    if (CEUtils.LineThroughRect(points[i], points[i - 1], targetHitbox, width)) {
                        return true;
                    }
                }
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            float alpha = 1;
            if (Projectile.timeLeft < 20) {
                alpha = (float)Projectile.timeLeft / 20f;
            }
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<Vector2> points = getPointsRelative(-40);
            List<Vector2> pointsOutside = getPointsRelative(40);
            int i;
            for (i = 0; i < points.Count; i++) {
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + points[i],
                      new Vector3((float)i / points.Count, 1, 1),
                      Color.White * alpha));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + pointsOutside[i],
                      new Vector3((float)i / points.Count, 0, 1),
                      Color.White * alpha));

            }
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3) {
                Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

    }

}