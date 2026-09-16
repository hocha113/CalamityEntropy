using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class BatteringRamProj : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 100;
            Projectile.penetrate = -1;
            Projectile.ArmorPenetration = 38;
        }
        public bool CanHit = false;
        public float charge { get { return Projectile.ai[0]; } set { Projectile.ai[0] = value; } }
        public int frame = 0;
        public int frameCounter = 0;
        public bool init = false;
        public override void AI() {
            if (!init) {
                init = true;
                Projectile.ai[2] = -1;
                Projectile.localAI[0] = -1;
                if (Main.zenithWorld)
                    Projectile.scale *= 2;
            }
            if (Projectile.ai[2] > 0) {
                if (Main.myPlayer != Projectile.owner) {
                    OnHitBothSide(((int)Projectile.ai[2]).ToNPC());
                }
                Projectile.ai[2] = 0;
            }
            if (Projectile.localAI[0] > 0) {
                if (Main.myPlayer != Projectile.owner && CanHit) {
                    OnHitBothSide(((int)Projectile.localAI[0]).ToPlayer());
                }
                Projectile.localAI[0] = 0;
            }
            if (frame > 0) {
                frameCounter++;
                if (frameCounter >= 6) {
                    frame++;
                    frameCounter = 0;
                    if (frame > 2) {
                        frame = 0;
                    }
                }
            }
            Player player = Projectile.GetOwner();
            player.Entropy().MouseWorldListener = true;
            player.heldProj = Projectile.whoAmI;
            if (Projectile.timeLeft > 1) {
                player.itemTime = 2;
                player.itemAnimation = 2;
            }

            if (charge < 1) {
                Projectile.velocity = (player.Entropy().MouseWorld - player.Center).normalize() * Projectile.velocity.Length();
                charge += 0.025f * player.GetTotalAttackSpeed(Projectile.DamageType) * (player.AzafureEnhance() ? 1.6f : 1);
                player.direction = Projectile.velocity.X > 0 ? 1 : -1;
                Projectile.rotation = (player.Entropy().MouseWorld - player.Center).ToRotation() - player.direction * ((float)Math.Cos(((float)Math.Cos(((float)Math.Cos(charge * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.ToRadians(150);
                Projectile.timeLeft = 70;
            }
            if (charge >= 1) {
                charge = 1;
                if (!player.channel) {
                    if (Projectile.ai[1] == 0) {
                        CanHit = true;
                        CEUtils.PlaySound("ExobladeDashImpact", 1, Projectile.Center, volume: 0.6f);
                        Projectile.ai[1]++;
                        player.mount.Dismount(player);
                        player.RemoveAllGrapplingHooks();
                        player.velocity = Projectile.velocity * 2 * (Main.zenithWorld ? -1 : 1);
                    }
                    if (Projectile.timeLeft > 52) {
                        Vector2 top = Projectile.Center + Projectile.velocity * 22;
                        int sparkLifetime2 = Main.rand.Next(36, 48);
                        float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                        Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.Firebrick, Main.rand.NextFloat(0, 1));
                        //PRT_LineCal Configure(false,lifetime)对齐Calamity LineParticle
                        PRTLoader.NewParticle<PRT_LineCal>(top, -Projectile.velocity.RotatedBy(0.2f) * 3.2f, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                        PRTLoader.NewParticle<PRT_LineCal>(top, -Projectile.velocity.RotatedBy(-0.2f) * 3.2f, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                        if (Projectile.timeLeft % 3 == 0) {
                            PRTLoader.NewParticle<PRT_ImpactParticle>(Projectile.Center + Projectile.velocity * 22, Projectile.velocity * -0.6f, Color.LightGoldenrodYellow, 0.16f)
                                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation());
                        }
                    }
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), CanHit ? 0.8f : 0.06f, false);
                }
                else {
                    Projectile.velocity = (player.Entropy().MouseWorld - player.Center).normalize() * Projectile.velocity.Length();
                    player.direction = Projectile.velocity.X > 0 ? 1 : -1;

                    Projectile.rotation = (player.Entropy().MouseWorld - player.Center).ToRotation() - player.direction * ((float)Math.Cos(((float)Math.Cos(((float)Math.Cos(charge * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.ToRadians(150);
                    Projectile.timeLeft = 70;
                }
                if (Projectile.timeLeft == 50 && CanHit) {
                    CanHit = false;
                    player.Entropy().XSpeedSlowdownTime = 26;
                    Projectile.timeLeft = 2;
                }
                if (Projectile.timeLeft == 32) {
                    frame++;
                    CEUtils.PlaySound("DudFire", 1, Projectile.Center);
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 44, Projectile.velocity.RotatedBy(-2f * player.direction).RotatedByRandom(0.26f), ModContent.ProjectileType<BatteringRamShell>(), 0, 0, player.whoAmI);
                    }
                }
                if (Projectile.timeLeft == 30) {
                    CEUtils.PlaySound("steam", 1, Projectile.Center);
                }
                if (Projectile.timeLeft <= 30 && Projectile.timeLeft > 4) {
                    float c = (Projectile.timeLeft - 4) / 26f;
                    Vector2 steamCenter = Projectile.Center + Projectile.rotation.ToRotationVector2() * 68 * Projectile.scale;
                    float rot = Projectile.rotation + player.direction * -2f;
                    for (int i = 0; i < 8; i++) {
                        var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter, rot.ToRotationVector2().RotatedByRandom(0.16f) * 8, Color.White * 0.42f * c, 0f);
                        p.timeleftmax = 36;
                        p.Lifetime = 36;
                        p.scaleEnd = Main.rand.NextFloat(0.06f, 0.16f);
                        p.vc = 0.94f;
                        p.Configure(0.03f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 36);
                        var p2 = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2().RotatedByRandom(0.16f) * 4, rot.ToRotationVector2().RotatedByRandom(0.16f) * 8, Color.White * 0.42f * c, 0.016f);
                        p2.timeleftmax = 36;
                        p2.Lifetime = 36;
                        p2.scaleEnd = Main.rand.NextFloat(0.06f, 0.16f);
                        p2.vc = 0.94f;
                        p2.Configure(0.01f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 36);
                    }
                    rot += player.direction * 3.8f;
                    for (int i = 0; i < 8; i++) {
                        var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter, rot.ToRotationVector2().RotatedByRandom(0.16f) * 8, Color.White * 0.42f * c, 0f);
                        p.timeleftmax = 36;
                        p.Lifetime = 36;
                        p.scaleEnd = Main.rand.NextFloat(0.06f, 0.16f);
                        p.vc = 0.94f;
                        p.Configure(0.03f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 36);
                        var p2 = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2().RotatedByRandom(0.16f) * 4, rot.ToRotationVector2().RotatedByRandom(0.16f) * 8, Color.White * 0.42f * c, 0.016f);
                        p2.timeleftmax = 36;
                        p2.Lifetime = 36;
                        p2.scaleEnd = Main.rand.NextFloat(0.06f, 0.16f);
                        p2.vc = 0.94f;
                        p2.Configure(0.01f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 36);
                    }
                }
            }
            Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));

        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Projectile.rotation);
            writer.Write(Projectile.localAI[0]);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            Projectile.rotation = reader.ReadSingle();
            Projectile.localAI[0] = reader.ReadSingle();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<MechanicalTrauma>(260);
            OnHitBothSide(target);
            if (Main.zenithWorld) {
                target.AddBuff(ModContent.BuffType<MaliciousCode>(), 460);
            }
            Projectile.ai[2] = target.whoAmI;
            CEUtils.SetShake(target.Center, 16);
            Projectile.GetOwner().ApplyDamageToNPC(target, damageDone, 0, 0, hit.Crit, Projectile.DamageType);
            Projectile.rotation += 0.36f * Projectile.GetOwner().direction;
            Projectile.netUpdate = true;
            Item FalseGun;
            int gunID = ItemID.Minishark;
            FalseGun = new Item();
            FalseGun.SetDefaults(gunID, true);
            FalseGun.damage = Projectile.damage / 10;
            // 数值模板原为灾厄 CosmicViperEngine（击退 6、弹速 10），常数内联
            FalseGun.knockBack = 6f;
            FalseGun.shootSpeed = 10f * 1.25f;
            FalseGun.consumeAmmoOnFirstShotOnly = false;
            FalseGun.consumeAmmoOnLastShotOnly = false;
            FalseGun.ArmorPenetration = Projectile.ArmorPenetration;
            FalseGun.DamageType = DamageClass.Melee;
            Player player = Projectile.GetOwner();
            for (int i = 0; i < 8; i++) {
                float r = Projectile.rotation + Main.rand.NextFloat(-0.5f, 0.5f);
                if (player.HasAmmo(FalseGun)) {
                    player.PickAmmo(FalseGun, out int projID, out float shootSpeed, out int damage, out float kb, out int _, false);
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center - r.ToRotationVector2() * shootSpeed, r.ToRotationVector2() * shootSpeed, projID, damage / 10, kb, player.whoAmI);
                    p.ToProj().DamageType = DamageClass.Melee;
                    if (!p.ToProj().usesLocalNPCImmunity) {
                        p.ToProj().usesLocalNPCImmunity = true;
                        p.ToProj().localNPCHitCooldown = 16;
                    }
                    p.ToProj().Entropy().hittingTarget = target.whoAmI;
                }
            }
        }
        public void OnHitBothSide(NPC target) {
            Projectile.GetOwner().Entropy().immune = 46;
            target.AddBuff(BuffID.BrokenArmor, 420);
            target.AddBuff(BuffID.OnFire, 420);

            Color impactColor = Main.rand.NextBool(3) ? Color.Firebrick : Color.OrangeRed;
            float impactParticleScale = Main.rand.NextFloat(1f, 1.75f) * 2.8f;
            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center, Vector2.Zero, impactColor, impactParticleScale).Configure(Color.OrangeRed, 8, 0.16f, 2f);

            for (int i = 0; i < 32; i++) {
                Vector2 top = target.Center;
                Vector2 sparkVelocity2 = Projectile.velocity.RotateRandom(0.8f) * 1.8f * Main.rand.NextFloat(0.3f, 1f);
                int sparkLifetime2 = Main.rand.Next(20, 28);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.LightGoldenrodYellow, Main.rand.NextFloat(0, 1));
                //跟AltSpark成对出现时寿命/速度系数是旧代码原值
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            for (int i = 0; i < 32; i++) {
                Vector2 top = target.Center;
                Vector2 sparkVelocity2 = -Projectile.velocity.RotateRandom(0.8f) * 1.2f * Main.rand.NextFloat(0.3f, 1f);
                int sparkLifetime2 = Main.rand.Next(14, 18);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.Crimson, Main.rand.NextFloat(0, 1));
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }

            CEUtils.PlaySound("gunshot_large", 1, target.Center);
            CEUtils.PlaySound("BRFire", 1, target.Center);
            CanHit = false;
            Projectile.GetOwner().velocity *= -1f;
            Projectile.GetOwner().velocity.Y *= 0.4f;
            Projectile.GetOwner().Entropy().XSpeedSlowdownTime = 26;
            Projectile.GetOwner().velocity.Y -= 6;
            Projectile.timeLeft = 50;
        }
        public void OnHitBothSide(Player target) {
            Projectile.GetOwner().Entropy().immune = 46;
            target.AddBuff(BuffID.BrokenArmor, 420);
            target.AddBuff(BuffID.OnFire, 420);

            Color impactColor = Main.rand.NextBool(3) ? Color.Firebrick : Color.OrangeRed;
            float impactParticleScale = Main.rand.NextFloat(1f, 1.75f) * 2.8f;
            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center, Vector2.Zero, impactColor, impactParticleScale).Configure(Color.OrangeRed, 8, 0.16f, 2f);

            for (int i = 0; i < 32; i++) {
                Vector2 top = target.Center;
                Vector2 sparkVelocity2 = Projectile.velocity.RotateRandom(0.8f) * 1.8f * Main.rand.NextFloat(0.3f, 1f);
                int sparkLifetime2 = Main.rand.Next(20, 28);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.LightGoldenrodYellow, Main.rand.NextFloat(0, 1));
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
            for (int i = 0; i < 32; i++) {
                Vector2 top = target.Center;
                Vector2 sparkVelocity2 = -Projectile.velocity.RotateRandom(0.8f) * 1.2f * Main.rand.NextFloat(0.3f, 1f);
                int sparkLifetime2 = Main.rand.Next(14, 18);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.8f);
                Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.Crimson, Main.rand.NextFloat(0, 1));
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }

            CEUtils.PlaySound("gunshot_large", 1, target.Center);
            CEUtils.PlaySound("BRFire", 1, target.Center);
            CanHit = false;
            Projectile.GetOwner().velocity *= -1f;
            Projectile.GetOwner().velocity.Y *= 0.4f;
            Projectile.GetOwner().Entropy().XSpeedSlowdownTime = 26;
            Projectile.GetOwner().velocity.Y -= 6;
            Projectile.timeLeft = 50;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(projHitbox.Center.ToVector2() - Projectile.rotation.ToRotationVector2() * -10 * Projectile.scale, projHitbox.Center.ToVector2() + Projectile.rotation.ToRotationVector2() * 186 * Projectile.scale, targetHitbox, (int)(46 * Projectile.scale));
        }
        public override bool? CanHitNPC(NPC target) {
            if (!CanHit) {
                return false;
            }
            return base.CanHitNPC(target);
        }

        public override bool CanHitPvp(Player target) {
            if (!CanHit) {
                return false;
            }
            if (Projectile.Colliding(Projectile.Hitbox, target.getRect())) {
                OnHitBothSide(target);
                Projectile.localAI[0] = target.whoAmI;
                CEUtils.SetShake(target.Center, 16);
                Projectile.rotation += 0.36f * Projectile.GetOwner().direction;
                Projectile.netUpdate = true;
            }
            return base.CanHitPvp(target);
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D whiteTex = CEUtils.getExtraTex("BatteringRamWhite");
            Texture2D tex = Projectile.GetTexture();
            float xs = ((float)Math.Cos(((float)Math.Cos(((float)Math.Cos(charge * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (float i = 0; i < 360; i += 60) {
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + (MathHelper.ToRadians(i) + Main.GameUpdateCount * 0.5f).ToRotationVector2() * 4, new Rectangle(0, tex.Height / 3 * frame, tex.Width, tex.Height / 3 - 2), Color.Lerp(Color.OrangeRed, Color.LightGoldenrodYellow, 0.5f + 0.5f * (float)Math.Cos(Main.GlobalTimeWrappedHourly)) * charge * charge * (Projectile.timeLeft < 56 ? 0 : 1), Projectile.velocity.X > 0 ? Projectile.rotation : Projectile.rotation + MathHelper.Pi, new Vector2(Projectile.velocity.X > 0 ? tex.Width * 0.12f : tex.Width * 0.88f, (tex.Height / 3f - 2) * 0.5f), Projectile.scale * new Vector2(1 - 0.6f * ((float)Math.Cos(xs * MathHelper.Pi - MathHelper.PiOver2)), 1), (Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally), 0);
            }
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, new Rectangle(0, tex.Height / 3 * frame, tex.Width, tex.Height / 3 - 2), lightColor, Projectile.velocity.X > 0 ? Projectile.rotation : Projectile.rotation + MathHelper.Pi, new Vector2(Projectile.velocity.X > 0 ? tex.Width * 0.12f : tex.Width * 0.88f, (tex.Height / 3f - 2) * 0.5f), Projectile.scale * new Vector2(1 - 0.6f * ((float)Math.Cos(xs * MathHelper.Pi - MathHelper.PiOver2)), 1), (Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            return false;
        }
    }


}