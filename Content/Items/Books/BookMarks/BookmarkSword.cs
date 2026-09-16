using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.ScreenShaker;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkSword : BookMark, IPriceFromRecipe
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Green;
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Sword");
        public override EBookProjectileEffect getEffect() {
            return new BookmarkSwordBMEffect();
        }

        public override Color tooltipColor => Color.Silver;
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.BreakerBlade)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        private static int projType = -1;
        public static int ProjType { get { if (projType == -1) { projType = ModContent.ProjectileType<BMSwordProjectile>(); } return projType; } }
    }

    public class BookmarkSwordBMEffect : EBookProjectileEffect
    {
    }
    public class BMSwordProjectile : BaseBookMinion
    {
        public override void SetDefaults() {
            base.SetDefaults();
            fixedBaseDamage = 50;
            Projectile.penetrate = -1;
            Projectile.light = 0.4f;
            Projectile.localNPCHitCooldown = -1;
            Projectile.width = Projectile.height = 46;
            Projectile.extraUpdates = 4;
        }
        public List<Vector2> OldPos = new();
        public List<float> OldRot = new();
        public bool Shake = true;
        public override bool PreDraw(ref Color lightColor) {
            if (trail != null) trail.DrawTrail(Main.spriteBatch);
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            Texture2D tex = Projectile.GetTexture();
            for (int i = 0; i < OldPos.Count; i++) {
                Main.EntitySpriteDraw(tex, OldPos[i] - Main.screenPosition, null, Color.White * 0.16f * ((float)i / OldPos.Count), OldRot[i] + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.rotation.ToRotationVector2() * Offset - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return (Projectile.Center + Projectile.rotation.ToRotationVector2() * Offset).getRectCentered(Projectile.width, Projectile.height).Intersects(targetHitbox);
        }
        public enum AIStyle
        {
            Chase,
            Strike,
            Smash,
            Idle,
            Stop
        }
        public AIStyle aiStyle = AIStyle.Idle;
        public NPC target = null;
        public int AttackDelay = 120;
        public int AttackTimer = 0;
        public float Offset = 0;
        public float num = 0;
        public float num2 = 0;
        public PRT_TrailParticle trail = null;
        public override float DamageMult => 0.45f;
        public override void AI() {
            base.AI();
            var player = Projectile.GetOwner();
            float tofs = target == null ? 120 : (target.width + target.height) / 2f + 120;
            if (Main.myPlayer != Projectile.owner || BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkSwordBMEffect>(player))
                Projectile.timeLeft = 3;
            Projectile.netUpdate = true;
            if (trail == null && ++Projectile.localAI[2] > 2) {
                //PRT_TrailParticle寿命-1,每帧手动Lifetime+AddPoint,Hammer书签同款写法
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.Purple * 0.8f, 1f * Projectile.scale);
                trail.maxLength = 42;
                trail.ShouldDraw = false;
                trail.Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0, -1);
            }
            if (Projectile.localAI[2] > 2) {
                trail.Lifetime = 24;
                trail.AddPoint(Projectile.Center + Projectile.rotation.ToRotationVector2() * (8 + Offset));
                trail.Opacity = aiStyle == AIStyle.Strike ? 1 : 0.7f;
                trail.Scale = float.Lerp(trail.Scale, Projectile.scale * ((aiStyle == AIStyle.Strike && AttackTimer > 20 && AttackTimer < 160) ? 4 : 0.8f), 0.02f);
            }
            if (target == null || !target.active || CEUtils.getDistance(target.Center, Projectile.Center) > 6000 || target.dontTakeDamage) {
                target = CEUtils.FindMinionTarget(Projectile, 3200);
            }
            float DelayMult = 1;
            if (ShooterModProjectile is EntropyBookHeldProjectile eb_) {
                DelayMult = eb_.CauculateAttackSpeed();
            }
            AttackTimer--;
            if (target == null && AttackTimer <= 2) {
                AttackTimer = 0;
                aiStyle = AIStyle.Idle;
            }
            else {
                if (aiStyle == AIStyle.Idle)
                    aiStyle = AIStyle.Chase;
            }
            Projectile.pushByOther(0.1f);
            if (target != null) {
                if (aiStyle == AIStyle.Chase) {
                    float targetRot = (target.Center - Projectile.Center).ToRotation();
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.03f, false);
                    Vector2 tpos = target.Center + (Projectile.Center - target.Center).normalize() * tofs;
                    if (CEUtils.getDistance(Projectile.Center, tpos) > 8)
                        Projectile.velocity += (tpos - Projectile.Center).normalize() * 1f;
                    Projectile.velocity *= 0.9f;
                    AttackDelay--;
                    if (AttackDelay <= 0) {
                        Projectile.ResetLocalNPCHitImmunity();
                        num2 = Main.rand.NextBool() ? 1 : -1;
                        AttackDelay = (int)(12 / DelayMult);
                        Shake = true;
                        if (Main.rand.NextBool()) {
                            aiStyle = AIStyle.Strike;
                            AttackTimer = 180;
                            num = Projectile.rotation;
                        }
                        else {
                            aiStyle = AIStyle.Smash;
                            AttackTimer = 180;
                            num = Projectile.rotation;
                        }
                    }
                }
            }
            if (aiStyle == AIStyle.Stop) {
                Projectile.velocity *= 0.96f;
                if (target != null) {
                    float targetRot = (target.Center - Projectile.Center).ToRotation();
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.06f, false);
                }
                if (AttackTimer <= 0) {
                    aiStyle = AIStyle.Chase;
                }
            }
            if (aiStyle == AIStyle.Smash) {
                if (AttackTimer == 179 && target != null)
                    Projectile.rotation = ((target.Center + target.velocity * 8) - Projectile.Center).ToRotation();
                if (AttackTimer < 160) {
                    Projectile.velocity *= 0.96f;
                    Projectile.velocity += Projectile.rotation.ToRotationVector2();
                }
                else {
                    Projectile.velocity *= 0.94f;
                    Projectile.velocity -= Projectile.rotation.ToRotationVector2() * 0.6f;
                }
                if (AttackTimer < 120)
                    AttackTimer = 0;
                if (AttackTimer <= 0) {
                    aiStyle = AIStyle.Stop;
                    AttackTimer = (int)(8 / DelayMult);
                    num = 0;
                }
            }
            if (aiStyle == AIStyle.Strike) {
                Projectile.velocity *= 0.8f;
                if (target != null) {
                    Vector2 tpos = target.Center + (Projectile.Center - target.Center).normalize() * tofs;

                    if (CEUtils.getDistance(Projectile.Center, tpos) > 8)
                        Projectile.velocity += (tpos - Projectile.Center).normalize() * 1f;
                }
                if (AttackTimer < 130)
                    Offset = float.Lerp(Offset, tofs, 0.046f);
                Projectile.rotation = num + num2 * MathHelper.ToRadians(600) * CEUtils.GetRepeatedCosFromZeroToOne(1 - AttackTimer / 180f, 2);
                if (AttackTimer <= 0) {
                    aiStyle = AIStyle.Stop;
                    AttackTimer = (int)(10 / DelayMult);
                    num = 0;
                }
            }
            else {
                Offset *= 0.94f;
            }
            if (aiStyle == AIStyle.Idle) {
                Vector2 taretPos = player.Center + new Vector2(100f * (float)Math.Sin(Main.GameUpdateCount * 0.05f), -140);
                float targetRot = -MathHelper.PiOver2 + Projectile.velocity.X * 0.3f;
                Projectile.velocity += (taretPos - Projectile.Center) * 0.001f;
                Projectile.velocity *= 0.98f;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.01f, false);
            }
            OldRot.Add(Projectile.rotation);
            OldPos.Add(Projectile.Center + Projectile.rotation.ToRotationVector2() * Offset);
            if (OldRot.Count > 32) {
                OldRot.RemoveAt(0);
                OldPos.RemoveAt(0);
            }

        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            base.ModifyHitNPC(target, ref modifiers);
            modifiers.SourceDamage *= aiStyle == AIStyle.Strike ? 0.7f : 1;
            modifiers.ArmorPenetration += 18;
        }
        public override bool? CanHitNPC(NPC target) {
            return (aiStyle == AIStyle.Strike || aiStyle == AIStyle.Smash) ? null : false;
        }
        public override void OnKill(int timeLeft) {
            base.OnKill(timeLeft);
            if (trail != null)
                trail.ShouldDraw = true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            CEUtils.PlaySound("ExoHit1", Main.rand.NextFloat(2f, 2.4f), Projectile.Center);
            CEUtils.PlaySound("metalhit", Main.rand.NextFloat(1.2f, 1.6f), Projectile.Center, volume: 0.5f);
            Color impactColor = Color.LightBlue;
            float impactParticleScale = Main.rand.NextFloat(2f, 2.4f);
            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center, Vector2.Zero, impactColor, impactParticleScale).Configure(new Color(140, 140, 255), 12, Main.rand.NextFloat(-0.1f, 0.1f), 2.5f);
            if (aiStyle == AIStyle.Smash) {
                for (int i = 0; i < 32; i++) {
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center, Projectile.velocity.normalize().RotatedByRandom(0.32f) * Main.rand.NextFloat(4, 16), Color.Lerp(new Color(236, 236, 236), Color.Blue, Main.rand.NextFloat()), Main.rand.NextFloat(0.4f, 1.2f)).Configure(false, 20);
                }
                for (int i = 0; i < 32; i++) {
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center, CEUtils.randomPointInCircle(16), Color.Lerp(new Color(236, 236, 236), Color.Blue, Main.rand.NextFloat()), Main.rand.NextFloat(0.4f, 1.2f)).Configure(false, 20);
                }
                if (Shake)
                    ScreenShaker.AddShake(new ScreenShake(Projectile.rotation.ToRotationVector2() * -1, Projectile.scale * 3 * Utils.Remap(CEUtils.getDistance(target.Center, Projectile.GetOwner().Center), 400, 1800, 1, 0)));
            }
            else {
                if (Shake)
                    ScreenShaker.AddShake(new ScreenShake(Vector2.Zero, Projectile.scale * 1 * Utils.Remap(CEUtils.getDistance(target.Center, Projectile.GetOwner().Center), 400, 1800, 1, 0)));
                for (int i = 0; i < 32; i++) {
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center, Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * num2).RotatedByRandom(0.32f) * Main.rand.NextFloat(4, 32), Color.Lerp(Color.Blue, Color.White, Main.rand.NextFloat()), Main.rand.NextFloat(0.4f, 1.2f)).Configure(false, 20);
                }
            }
            Shake = false;
        }
    }
}