using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ForeseeWhipProj : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 6;
            this.segments = 30;
            this.rangeMult = 2f;
        }
        public override string getTagEffectName => "ForeseeWhip";
        public override SoundStyle? WhipSound => null;
        public override void WhipAI() {
            base.WhipAI();
            float RotF = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + RotF * CEUtils.GetRepeatedCosFromZeroToOne((Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates)), 1)) * -1 * (Projectile.velocity.X > 0 ? -1 : 1);
        }
        public override Color StringColor => Color.Transparent;
        public override void DrawStrings(List<Vector2> points) { }
        public List<Vector2> getPoints(float perc) {
            var points = new List<Vector2>();
            Projectile.GetWhipSettings(Projectile, out float ttfo, out int segs, out float rangeMult);
            float c = (float)((Math.Cos(perc * MathHelper.TwoPi - MathHelper.Pi) + 1) * 0.5f);
            rangeMult *= Projectile.GetOwner().whipRangeMultiplier;
            for (int i = 0; i < segs; i++) {
                points.Add(Vector2.Lerp(Projectile.GetOwner().HandPosition.Value, Projectile.GetOwner().HandPosition.Value + Projectile.rotation.ToRotationVector2() * rangeMult * c * 260, i / (float)(segs - 1)) + Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12 + i * 0.26f) * (i / 31f) * 260 * c * rangeMult * 0.1f);
            }
            return points;
        }
        public override void ModifyControlPoints(List<Vector2> points) {
            points.Clear();
            List<Vector2> p2 = getPoints(Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates));

            foreach (var p in p2) {
                points.Add(p);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            if (Projectile.localAI[2]++ == 0) {
                CEUtils.PlaySound("ProphetWhipHitShine", 1, target.Center, volume: 0.3f);
                CEUtils.PlaySound("runesonghit", 2, target.Center, volume: 0.3f);
            }
            target.AddBuff(ModContent.BuffType<ForeseeWhipDebuff>(), 240);
        }
        public override bool PreDraw(ref Color lightColor) {
            base.PreDraw(ref lightColor);
            return false;
        }
        public override void DrawSegs(List<Vector2> points) {
            {
                int i = 0;
                int frameY = 0;
                int frameHeight = 0;
                Vector2 origin = Vector2.Zero;
                this.getFrame(i, points.Count, ref frameY, ref frameHeight, ref origin);
                float drawScale = Projectile.scale * this.getSegScale(i, points.Count);
                Vector2 lightPos = i == 0 ? points[i] : Vector2.Lerp(points[i - 1], points[i], 0.5f);
                Color color = Color.Lerp(Lighting.GetColor((int)(lightPos.X / 16f), (int)(lightPos.Y / 16f)), this.StringColor, Projectile.light);

                float rot = 0;
                if (i == points.Count - 1)
                    rot = (points[i + 1] - points[i]).ToRotation();

                rot -= MathHelper.PiOver2;
                Main.EntitySpriteDraw(Projectile.GetTexture(), points[i] - Main.screenPosition, new Rectangle(0, frameY, Projectile.GetTexture().Width, frameHeight), color, rot, origin, drawScale, Projectile.spriteDirection > 0 ? Microsoft.Xna.Framework.Graphics.SpriteEffects.None : Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally);


            }
            {
                float lc = 1;
                float jn = 0;

                List<ColoredVertex> ve = new();
                for (int i = 1; i < points.Count - 1; i++) {
                    Color cl = Lighting.GetColor(new Point((int)(points[i].X / 16), (int)(points[i].Y / 16)));
                    jn = (float)(i - 1) / (points.Count - 2);
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * lc,
                          new Vector3(jn, 1, 1),
                          cl));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * lc,
                          new Vector3(jn, 0, 1),
                          cl));
                }
                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = CEUtils.getExtraTex("ForeseeWhip");
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }


        }
        public override int handleHeight => 50;
        public override int segHeight => 22;
        public override int endHeight => 34;
        public override int segTypes => 2;
    }
}