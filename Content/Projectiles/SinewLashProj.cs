using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Utilities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class SinewLashProj : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 1;
            this.segments = 20;
            this.rangeMult = 1.8f;
        }
        public override string getTagEffectName => "SinewLash";
        public override SoundStyle? WhipSound => null;
        public Rope WhipPoints = null;
        public bool Forwarding = true;
        public override bool PreAI() {
            Player player = Projectile.GetOwner();
            if (WhipPoints == null) {
                Projectile.GetWhipSettings(Projectile, out float num1, out int segCount, out float num2);
                WhipPoints = new Rope(player.HandPosition.Value, player.HandPosition.Value, segCount, 0, Vector2.Zero, 0.01f, 15, false);
            }
            if (Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates) <= 0.7f) {
                WhipPoints.segmentLength = CEUtils.getDistance(getMyEndPos(), player.HandPosition.Value) / (WhipPoints.GetPoints().Count);
                WhipPoints.End = getMyEndPos();
            }
            WhipPoints.Start = player.HandPosition.Value;

            if (Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates) > 0.7f) {
                if (Forwarding) {
                    SegLengthMax = CEUtils.getDistance(WhipPoints.GetPoints()[WhipPoints.GetPoints().Count - 1], player.HandPosition.Value);
                    Forwarding = false;
                    WhipPoints.twoPoint = false;
                }
                float s = 1 - (((Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates)) - 0.7f) / 0.3f);
                Projectile.GetWhipSettings(Projectile, out float num1, out int segCount, out float num2);
                WhipPoints.segmentLength = SegLengthMax * (s * s) / segCount;

            }
            WhipPoints.Update();
            return base.PreAI();
        }
        public override bool HitboxActive() {
            if ((Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates)) < 0.7f) {
                return false;
            }
            return base.HitboxActive();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            ScreenShaker.AddShake(new ScreenShaker.NoDirQuickShake(7));
            base.OnHitNPC(target, hit, damageDone);
            CEUtils.PlaySound("FleshWhipHit", Main.rand.NextFloat(0.8f, 1.2f), EndPoint);
            target.AddBuff(ModContent.BuffType<SinewLashWhipDebuff>(), 240);
            for (int i = 0; i < 3; i++) {
                Color impactColor = Main.rand.NextBool(3) ? Color.LightCoral : Color.Crimson;
                float impactParticleScale = Main.rand.NextFloat(1f, 1.75f);
                //PRT_SparkleCal bloom/color在Configure里,旧Calamity SparkleParticle两色构造
                PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, impactParticleScale).Configure(Color.Red, 8, 0, 2.5f);
            }

            float sparkCount = 8;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = (-Projectile.velocity * 3).RotatedByRandom(0.24f) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime2 = Main.rand.Next(23, 35);
                float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                Color sparkColor2 = Main.rand.NextBool(3) ? Color.Red : Color.IndianRed;
                if (Main.rand.NextBool()) {
                    //AltSpark Configure(bool,int)是Ports签名,不是opacity/glow/mode那套
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) - Projectile.velocity * 1.2f * 3, sparkVelocity2 * 1, sparkColor2, sparkScale2 * 1.4f).Configure(false, (int)(sparkLifetime2 * 1.2f));
                }
                else {
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) - Projectile.velocity * 1.2f * 3, sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), Main.rand.NextBool() ? Color.Red : Color.Firebrick, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)));
                }
            }
            float dustCount = 42;
            for (int i = 0; i <= dustCount; i++) {
                int dustID = Main.rand.NextBool(3) ? DustID.t_Flesh : DustID.Blood;
                Dust dust2 = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), dustID, (-Projectile.velocity * 3).RotatedBy(0).RotatedByRandom(0.55f) * Main.rand.NextFloat(0.3f, 1.1f));
                dust2.scale = Main.rand.NextFloat(0.9f, 2.4f);
                dust2.noGravity = false;
            }
        }
        public Vector2 getMyEndPos() {
            Vector2 p = Projectile.GetOwner().HandPosition.Value;
            float c = (Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates)) / 0.7f;
            float ha = 0;
            ha = new Vector2(-180, 0).RotatedBy(c * MathHelper.Pi).Y;

            Projectile.GetWhipSettings(Projectile, out float num1, out int num2, out float rangeMult);
            p += Projectile.velocity * (float)Math.Sqrt(3400 * c) * rangeMult * Projectile.GetOwner().whipRangeMultiplier;
            p += new Vector2(0, ha * Projectile.spriteDirection).RotatedBy(Projectile.velocity.ToRotation());
            return p;
        }
        public float SegLengthMax = 0;
        public override Color StringColor => Color.DarkRed;
        public override void ModifyControlPoints(List<Vector2> points) {
            if (WhipPoints != null) {
                points.Clear();
                foreach (var point in WhipPoints.GetPoints()) {
                    points.Add(point);
                }
            }
        }
        public override void DrawSegs(List<Vector2> points) {
            for (int i = 0; i < points.Count; i++) {
                int frameY = 0;
                int frameHeight = 0;
                Vector2 origin = Vector2.Zero;
                this.getFrame(i, points.Count, ref frameY, ref frameHeight, ref origin);
                float drawScale = Projectile.scale * this.getSegScale(i, points.Count);
                Vector2 lightPos = i == 0 ? points[i] : Vector2.Lerp(points[i - 1], points[i], 0.5f);
                Color color = Color.Lerp(Lighting.GetColor((int)(lightPos.X / 16f), (int)(lightPos.Y / 16f)), this.StringColor, Projectile.light);

                float rot = 0;
                float YScale = 1;
                if (i > 0 && i < points.Count - 1) {
                    YScale = CEUtils.getDistance(points[i - 1], points[i]) / (frameHeight - 2);
                }
                if (i == points.Count - 1) {
                    rot = (points[i] - points[i - 1]).ToRotation();
                }
                else {
                    rot = (points[i + 1] - points[i]).ToRotation();
                }
                rot -= MathHelper.PiOver2;
                Main.EntitySpriteDraw(Projectile.GetTexture(), points[i] - Main.screenPosition, new Rectangle(0, frameY, Projectile.GetTexture().Width, frameHeight), color, rot, origin, new Vector2(float.Min(drawScale / YScale, drawScale
                     * 1.2f), YScale), Projectile.spriteDirection > 0 ? Microsoft.Xna.Framework.Graphics.SpriteEffects.None : Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally);
            }
        }

        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates) < 0.3f) {
                return false;
            }
            return base.CanHitNPC(target);
        }

        public override int handleHeight => 28;
        public override int segHeight => 20;
        public override int endHeight => 24;
        public override int segTypes => 2;
    }

    public class FleshChunk : ModProjectile
    {
        public override void SetStaticDefaults() {
            //ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, 1);
            Projectile.width = Projectile.height = 16;
            Projectile.timeLeft = 180;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center - Projectile.velocity, targetHitbox, 16);
        }
        public override bool? CanHitNPC(NPC target) {
            return Projectile.ai[0] > 38 ? null : false;
        }
        public override void AI() {
            if (Projectile.ai[0] == 0)
                SoundEngine.PlaySound(SoundID.NPCDeath1 with { Pitch = 0.4f }, Projectile.Center);
            if (Projectile.ai[0] < 38) {
                Projectile.rotation += Projectile.velocity.X * 0.012f;
                Projectile.velocity *= 0.96f;
            }
            else {
                if (Projectile.ai[1] < 1) {
                    Projectile.ai[1] += 0.1f;
                    if (Projectile.ai[1] > 1)
                        Projectile.ai[1] = 1;
                }
                if (Projectile.ai[0] > 52) {
                    NPC target = Projectile.FindMinionTarget(1400);
                    if (target != null) {
                        Projectile.velocity *= 0.97f;
                        Projectile.velocity += (target.Center - Projectile.Center).normalize() * 6;
                    }
                    else {
                        Projectile.Kill();
                    }

                }

            }
            Projectile.ai[0]++;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.ai[1] > 0 && Projectile.ai[1] < 1) {
                Texture2D texr = CEExtraAssets.BloomRing;
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.spriteBatch.Draw(texr, Projectile.Center - Main.screenPosition, null, new Color(255, 160, 160) * Projectile.ai[1], 0, texr.Size() / 2f, 1 - Projectile.ai[1], SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tex = Projectile.ai[2] == 0 ? Projectile.GetTexture() : this.getTextureAlt();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, tex));
            return false;
        }
        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCDeath19 with { Pitch = 0f }, Projectile.Center);
            for (int i = 0; i < 128; i++) {
                Vector2 v = (-Projectile.velocity.RotatedByRandom(1.6f)).normalize() * Main.rand.NextFloat(12);
                var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood);
                d.scale = 1.2f;
                d.velocity = v;
            }
            for (int i = 0; i < 4; i++)
                Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomPointInCircle(6), Mod.Find<ModGore>($"FleshGore{(Main.rand.NextBool() ? "" : "2")}").Type);
        }
    }
}