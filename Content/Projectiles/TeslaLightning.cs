using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Utilities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class TeslaLightning : ModProjectile
    {
        public Vector2 endPos;
        List<Vector2> points = null;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 12;
            Projectile.timeLeft = 16;
        }
        public override void AI() {
            if (Projectile.localAI[0] == 0) {
                CEUtils.PlaySound("spark", 1, Projectile.Center, 1);
            }
            Vector2 end = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (points == null || Projectile.localAI[0] % 5 == 0) {
                points = LightningGenerator.GenerateLightning(Projectile.Center, end, 36, 6);
            }
            Projectile.localAI[0]++;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (points == null) return false;
            for (int i = 1; i < points.Count; i++) {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 4)) {
                    return true;
                }
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.ai[2] == 1) {
                target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 240);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tx = CEExtraAssets.MegaStreakBacking2;
            if (points == null) {
                return false;
            }
            float width = Projectile.timeLeft / 16f;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = new Color(255, 60, 60);
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * width,
                      new Vector3(0, 1, 1),
                    b));
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * width,
                  new Vector3(0, 0, 1),
                  b));
            for (int i = 1; i < points.Count; i++) {
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * width,
                      new Vector3((float)(i + 1) / points.Count, 1, 1),
                    b));
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * width,
                      new Vector3((float)(i + 1) / points.Count, 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            ve = new List<ColoredVertex>();
            b = new Color(255, 240, 240);
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 10 * width,
                      new Vector3(0, 1, 1),
                    b));
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 10 * width,
                  new Vector3(0, 0, 1),
                  b));
            for (int i = 1; i < points.Count; i++) {
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 10 * width,
                      new Vector3((float)(i + 1) / points.Count, 1, 1),
                    b));
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 10 * width,
                      new Vector3((float)(i + 1) / points.Count, 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
    public class TeslaLightningRed : ModProjectile
    {
        public Vector2 endPos;
        List<Vector2> points = null;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 12000;
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
            Projectile.ArmorPenetration = 12;
            Projectile.timeLeft = 16;
        }
        public override void AI() {
            Vector2 end = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (Projectile.localAI[0] == 0) {
                CEUtils.PlaySound("spark", 1, Projectile.Center, 1);
                //PRT_ShineParticle FollowOwner字段spawn后赋,Configure只管Additive和lifetime
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.16f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 18);
                PRTLoader.NewParticle<PRT_ShineParticle>(end, Vector2.Zero, Color.White, 0.16f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 18);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.16f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 18);
                //ShineParticle FollowOwner字段spawn后赋,Configure只管Additive和lifetime
                PRTLoader.NewParticle<PRT_ShineParticle>(end, Vector2.Zero, Color.White, 0.16f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 18);

            }

            if (points == null || Projectile.localAI[0] % 5 == 0) {
                points = LightningGenerator.GenerateLightning(Projectile.Center, end, 36, 6);
            }
            Projectile.localAI[0]++;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (points == null) return false;
            for (int i = 1; i < points.Count; i++) {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 4)) {
                    return true;
                }
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.ai[2] == 1) {
                target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 240);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tx = CEExtraAssets.MegaStreakBacking2;
            if (points == null) {
                return false;
            }
            float width = Projectile.timeLeft / 16f;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = new Color(240, 240, 255);
            if (Projectile.ai[2] == 1) {
                b = Color.Red;
            }
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * width,
                      new Vector3(0, 1, 1),
                    b));
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * width,
                  new Vector3(0, 0, 1),
                  b));
            for (int i = 1; i < points.Count; i++) {
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * width,
                      new Vector3((float)(i + 1) / points.Count, 1, 1),
                    b));
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * width,
                      new Vector3((float)(i + 1) / points.Count, 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            ve = new List<ColoredVertex>();
            b = new Color(240, 240, 255);
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 7 * width,
                      new Vector3(0, 1, 1),
                    b));
            ve.Add(new ColoredVertex(points[0] - Main.screenPosition + (points[1] - points[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 7 * width,
                  new Vector3(0, 0, 1),
                  b));
            for (int i = 1; i < points.Count; i++) {
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 7 * width,
                      new Vector3((float)(i + 1) / points.Count, 1, 1),
                    b));
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 7 * width,
                      new Vector3((float)(i + 1) / points.Count, 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
