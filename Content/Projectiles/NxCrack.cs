using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NxCrack : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.ArmorPenetration = 100;

        }
        float w = 0.1f;
        float wj = 0.3f;
        public override void AI() {
            w += wj;
            wj -= 0.065f;
            if (w < 0) {
                Projectile.Kill();
                return;
            }
            Projectile.ai[0]++;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Player player = Main.player[Projectile.owner];
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity * 600, targetHitbox, 60);
        }
        public override bool PreDraw(ref Color dc) {
            return false;
        }
        public void drawCrack() {
            List<float> c = new List<float>() { 0, 1.2f, 0.8f, 1f, 1.27f, 0.9f, 1.2f, 1f, 0.8f, 0 };
            List<float> d = new List<float>() { 0, 0.3f, 0.56f, 0.8f, 0.94f, 0.94f, 0.8f, 0.56f, 0.3f, 0 };
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = Color.White;
            for (int i = 0; i < c.Count; i++) {
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + Projectile.velocity * (i * (600f / c.Count)) + new Vector2(0, -1).RotatedBy(Projectile.rotation) * d[i] * 0.7f * (24 + (float)(Math.Cos(c[i] * Main.GlobalTimeWrappedHourly * 3) + 1) * 2) * w + new Vector2(0, 1).RotatedBy(Projectile.rotation) * (float)Math.Cos(c[c.Count - 1 - i] * Main.GlobalTimeWrappedHourly * 4) * d[d.Count - 1 - i] * 0.4f * 2 * w,
                          new Vector3((float)i / c.Count, 1, 1),
                          b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + Projectile.velocity * (i * (600f / c.Count)) + new Vector2(0, 1).RotatedBy(Projectile.rotation) * d[i] * 0.7f * (24 + (float)(Math.Cos(c[i] * Main.GlobalTimeWrappedHourly * 3) + 1) * 2) * w + new Vector2(0, 1).RotatedBy(Projectile.rotation) * (float)Math.Cos(c[c.Count - 1 - i] * Main.GlobalTimeWrappedHourly * 4) * d[d.Count - 1 - i] * 0.4f * 2 * w,
                          new Vector3((float)i / c.Count, 1, 1),
                          b));
            }
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3) {
                Texture2D tx = CEExtraAssets.white;
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
        }
        public override bool? CanCutTiles() {
            return false;
        }
    }


}