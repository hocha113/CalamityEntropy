using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class SolarStormHeld : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 4;
            Projectile.DefineSynchronousData(Common.SyncDataType.Float, "shotCounter", 0f);
        }
        public float shotCounter { get { return Projectile.GetSyncValue<float>("shotCounter"); } set { Projectile.SetSyncValue("shotCounter", value); } }

        int ammoID = -1;
        public float tShooted = 0;
        public float ShootNeededTime = 30;
        public float maxChargeTime = 250;
        public bool spawnParti = true;
        public int steamTime = 0;
        public override void AI() {
            Player player = Projectile.owner.ToPlayer();
            if (Projectile.velocity.X > 0) {
                player.direction = 1;
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                player.direction = -1;
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            if (player.dead) {
                Projectile.Kill();
            }

            if (Projectile.owner == Main.myPlayer && Main.mouseLeft && !player.mouseInterface) {
                player.channel = true;
            }

            if (player.channel) {
                Projectile.timeLeft = 3;
            }
            player.itemTime = player.itemAnimation = 3;
            player.heldProj = Projectile.whoAmI;
            HandleChannelMovement(player, player.MountedCenter);
            if (player.HasAmmo(player.HeldItem)) {
                player.PickAmmo(player.HeldItem, out int projID, out float shootSpeed, out int damage, out float kb, out ammoID, true);
                shotCounter += player.GetTotalAttackSpeed(Projectile.DamageType);
                if (shotCounter < maxChargeTime) {
                    for (int l = 0; l < 1 + 30 * ((float)shotCounter / maxChargeTime); l++) {
                        //TargetPos/followOwner旧初始化器字段,Configure只管opacity和AdditiveBlend分桶
                        var glowSpark = PRTLoader.NewParticle<PRT_GlowSparkDirecting>(
                            Projectile.Center + Projectile.rotation.ToRotationVector2() * 16 + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(60, 120),
                            Vector2.Zero,
                            Color.OrangeRed,
                            Main.rand.NextFloat(0.6f, 1.4f) * 0.06f)
                            .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, Main.rand.Next(10, 16));
                        glowSpark.TargetPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 16;
                        glowSpark.followOwner = player;
                        glowSpark.scaleX = Main.rand.NextFloat(1, 2);
                    }
                }
                if (shotCounter < maxChargeTime && shotCounter >= tShooted + ShootNeededTime) {
                    tShooted += ShootNeededTime;
                    ShootNeededTime -= 1.6f;
                    if (Main.myPlayer == Projectile.owner) {
                        player.PickAmmo(player.HeldItem, out projID, out shootSpeed, out damage, out kb, out ammoID, false);

                        if (Main.zenithWorld) {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.One) * shootSpeed * 0.4f, ModContent.ProjectileType<SolarStormExplosionProj>(), damage * 4, kb * 2, player.whoAmI);
                        }
                        else {
                            for (int i = 0; i < 4; i++) {
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + (CEUtils.randomPointInCircle(36) * new Vector2(1, 0.6f)).RotatedBy(Projectile.rotation) + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(-64, 152), Projectile.velocity.SafeNormalize(Vector2.One) * shootSpeed, projID, damage, kb, player.whoAmI);
                            }
                        }
                        var snd = SoundID.DD2_BallistaTowerShot;
                        snd.MaxInstances = 5;
                        SoundEngine.PlaySound(snd, Projectile.Center);
                    }
                }
                if (shotCounter > maxChargeTime && spawnParti) {
                    spawnParti = false;
                    Color impactColor = Color.OrangeRed;
                    float impactParticleScale = 2f;
                    PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 16, Vector2.Zero, impactColor * 0.34f, impactParticleScale).Configure(Color.OrangeRed * 0.34f, 14, 0f, 3f);

                    var __prt = PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 16, Vector2.Zero, new Color(255, 160, 144), 1.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
                    __prt.FollowOwner = player;
                    var __prt2 = PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 16, Vector2.Zero, new Color(255, 210, 196), 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
                    __prt2.FollowOwner = player;

                }
                if (shotCounter > maxChargeTime + 36) {
                    shotCounter = 0;
                    tShooted = 0;
                    ShootNeededTime = 30;
                    spawnParti = true;
                    steamTime = 22;
                    CEUtils.PlaySound("ProminenceShoot", 1, Projectile.Center);
                    if (Main.zenithWorld) {
                        for (int i = 0; i < 4; i++) {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + CEUtils.randomPointInCircle(36) + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(-84, 84), Projectile.velocity.SafeNormalize(Vector2.One) * shootSpeed, projID, damage * 10, kb, player.whoAmI);
                        }
                    }
                    else {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.One) * shootSpeed * 0.4f, ModContent.ProjectileType<SolarStormExplosionProj>(), damage * 16, kb * 2, player.whoAmI);
                    }
                }

            }
            if (steamTime-- > 0 && steamTime <= 10) {
                if (steamTime == 10) {
                    CEUtils.PlaySound("steam", 1, Projectile.Center, 1, 0.8f);
                }

                float c = steamTime / 10f;
                Vector2 steamCenter = Projectile.Center + new Vector2(38, 24).RotatedBy(Projectile.rotation) * Projectile.scale;
                float rot = Projectile.rotation;
                //蒸汽烟旧对象初始化器拆成字段+Configure,SetProperty在Configure前先跑,顺序别反
                for (int i = 0; i < 16; i++) {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2() * (20 * (i / 16f)), rot.ToRotationVector2().RotatedByRandom(0.16f) * 20, Color.White * 0.42f * c, 0.018f);
                    p.timeleftmax = 16;
                    p.Lifetime = 16;
                    p.scaleEnd = Main.rand.NextFloat(0.06f, 0.1f);
                    p.vc = 0.85f;
                    p.Configure(0.018f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                }

                steamCenter = Projectile.Center + new Vector2(38, -24).RotatedBy(Projectile.rotation) * Projectile.scale;
                rot = Projectile.rotation;
                for (int i = 0; i < 16; i++) {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2() * (20 * (i / 16f)), rot.ToRotationVector2().RotatedByRandom(0.16f) * 20, Color.White * 0.42f * c, 0.018f);
                    p.timeleftmax = 16;
                    p.Lifetime = 16;
                    p.scaleEnd = Main.rand.NextFloat(0.06f, 0.1f);
                    p.vc = 0.85f;
                    p.Configure(0.018f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                }

                steamCenter = Projectile.Center + new Vector2(32, 43).RotatedBy(Projectile.rotation) * Projectile.scale;
                rot = Projectile.rotation + MathHelper.ToRadians(45);
                for (int i = 0; i < 16; i++) {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2() * (20 * (i / 16f)), rot.ToRotationVector2().RotatedByRandom(0.16f) * 20, Color.White * 0.42f * c, 0.018f);
                    p.timeleftmax = 16;
                    p.Lifetime = 16;
                    p.scaleEnd = Main.rand.NextFloat(0.06f, 0.1f);
                    p.vc = 0.85f;
                    p.Configure(0.018f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                }

                steamCenter = Projectile.Center + new Vector2(32, -43).RotatedBy(Projectile.rotation) * Projectile.scale;
                rot = Projectile.rotation - MathHelper.ToRadians(45);
                for (int i = 0; i < 16; i++) {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2() * (20 * (i / 16f)), rot.ToRotationVector2().RotatedByRandom(0.16f) * 20, Color.White * 0.42f * c, 0.018f);
                    p.timeleftmax = 16;
                    p.Lifetime = 16;
                    p.scaleEnd = Main.rand.NextFloat(0.06f, 0.1f);
                    p.vc = 0.85f;
                    p.Configure(0.018f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                }

                steamCenter = Projectile.Center + new Vector2(18, 55).RotatedBy(Projectile.rotation) * Projectile.scale;
                rot = Projectile.rotation + MathHelper.ToRadians(70);
                for (int i = 0; i < 16; i++) {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2() * (20 * (i / 16f)), rot.ToRotationVector2().RotatedByRandom(0.16f) * 20, Color.White * 0.42f * c, 0.018f);
                    p.timeleftmax = 16;
                    p.Lifetime = 16;
                    p.scaleEnd = Main.rand.NextFloat(0.06f, 0.1f);
                    p.vc = 0.85f;
                    p.Configure(0.018f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                }

                steamCenter = Projectile.Center + new Vector2(18, -55).RotatedBy(Projectile.rotation) * Projectile.scale;
                rot = Projectile.rotation - MathHelper.ToRadians(70);
                for (int i = 0; i < 16; i++) {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2() * (20 * (i / 16f)), rot.ToRotationVector2().RotatedByRandom(0.16f) * 20, Color.White * 0.42f * c, 0.018f);
                    p.timeleftmax = 16;
                    p.Lifetime = 16;
                    p.scaleEnd = Main.rand.NextFloat(0.06f, 0.1f);
                    p.vc = 0.85f;
                    p.Configure(0.018f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 16);
                }
            }
        }
        public void HandleChannelMovement(Player player, Vector2 playerRotatedPoint) {
            Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY;

            float speed = 16f;
            Vector2 newVelocity = (Main.MouseWorld - playerRotatedPoint).SafeNormalize(Vector2.UnitX * player.direction) * speed;

            if (Projectile.velocity.X != newVelocity.X || Projectile.velocity.Y != newVelocity.Y) {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = newVelocity;
            Projectile.rotation = Projectile.velocity.ToRotation();
            float light = ((float)shotCounter / maxChargeTime);
            Lighting.AddLight(Projectile.Center, light, light, light);
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = this.getTextureGlow();
            Rectangle frame = CEUtils.GetCutTexRect(tex, 10, ((int)Main.GameUpdateCount / 4) % 10, false);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation + ((Projectile.velocity.X > 0) ? 0 : MathHelper.Pi), new Vector2(((Projectile.velocity.X > 0) ? 40 : (tex.Width - 40)), 71), Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation + ((Projectile.velocity.X > 0) ? 0 : MathHelper.Pi), new Vector2(((Projectile.velocity.X > 0) ? 40 : (tex.Width - 40)), 71), Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            DrawChargingEnergyBall(Projectile.Center + Projectile.rotation.ToRotationVector2() * 16, Main.rand.NextFloat(0.8f, 1.2f) * float.Min(1, ((float)shotCounter / maxChargeTime)) * 1.8f, float.Min(1, (float)shotCounter / maxChargeTime * 6));
            return false;
        }
        public static void DrawChargingEnergyBall(Vector2 pos, float size, float alpha) {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, Color.OrangeRed * alpha, 0, tex.Size() * 0.5f, size * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 220, 190) * alpha, 0, tex.Size() * 0.5f, size * 0.24f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
    }

    public class SolarStormExplosionProj : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.extraUpdates = 11;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 5000;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }
        public void Explode() {
            Projectile.tileCollide = false;
            Projectile.localNPCHitCooldown = -1;
            Projectile.Resize(700, 700);
            Projectile.timeLeft = 280;
            Projectile.velocity *= 0;
            SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.timeLeft > 280) {
                SolarStormHeld.DrawChargingEnergyBall(Projectile.Center, 1.6f, 1);
            }
            else {
                SolarStormHeld.DrawChargingEnergyBall(Projectile.Center, 12 * (float)(Math.Cos(MathHelper.Pi * (Projectile.timeLeft / 280f) - MathHelper.PiOver2)), 1);
            }
            return false;
        }
        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (Projectile.timeLeft > 280) {
                Explode();
            }
            return false;
        }
        private void SpawnDust() {
            int coreDustCount = 2; //3
            int coreDustType = 262;
            for (int i = 0; i < coreDustCount; ++i) {
                float scale = Main.rand.NextFloat(1.0f, 1.4f);
                int idx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, coreDustType);
                Main.dust[idx].velocity *= 0.7f;
                Main.dust[idx].velocity += Projectile.velocity * 1.4f;
                Main.dust[idx].scale = scale;
                Main.dust[idx].noGravity = true;
            }

            int trailDustCount = 4;
            int trailDustType = 264;
            for (int i = 0; i < trailDustCount; ++i) {
                float scale = Main.rand.NextFloat(1.0f, 1.4f);
                int idx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, trailDustType, 0f, 0f);
                Main.dust[idx].velocity = Projectile.velocity * 0.8f;
                Main.dust[idx].scale = scale;
                Main.dust[idx].noGravity = true;
            }
        }
        public Color mainColor = Color.LawnGreen;
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.timeLeft > 100 && Projectile.timeLeft < 280) {
                return false;
            }
            return null;
        }
        public override void AI() {
            if (Projectile.timeLeft == 281) {
                Explode();
            }
            if (Projectile.timeLeft == 100) {
                if (!Main.dedServ) {
                    if (CEUtils.getDistance(Projectile.Center, CEUtils.GetOwner(Projectile).Center) < 2000) {
                        CalamityEntropy.FlashEffectStrength = 0.42f;
                        // 原写法只对持有者本端生效,保持该语义
                        if (Main.myPlayer == Projectile.owner)
                            ScreenShaker.AddShake(new ScreenShaker.NoDirQuickShake(12));
                    }
                    SoundEngine.PlaySound(SoundID.NPCDeath56, Projectile.Center);
                }
                for (int i = 0; i < 25; i++) {
                    Vector2 randVel = CEUtils.randomPointInCircle(24) * Main.rand.NextFloat(0.05f, (Main.rand.NextBool(3) ? 1f : 0.5f));
                    PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center + randVel, randVel, new Color(57, 46, 115) * 0.6f, Main.rand.NextFloat(0.9f, 2.3f)).Configure(0.5f, Main.rand.Next(25, 35 + 1));
                }
                for (int i = 0; i < 150; i++) {
                    Vector2 randVel = new Vector2(3, 3).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 1.6f);
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center + randVel, 303, randVel);
                    dust2.scale = Main.rand.NextFloat(1.75f, 2.5f);
                    dust2.noGravity = true;
                    dust2.color = new Color(57, 46, 115);
                    dust2.alpha = Main.rand.Next(40, 100 + 1);
                }

                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, mainColor, 4.5f * 0.16f).Configure("CalamityEntropy/Assets/Particles/LargeBloom", new Vector2(1, 1), Main.rand.NextFloat(-10, 10), 4.5f * 0.16f, 3.5f * 0.16f, 20);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.White, 4f * 0.16f).Configure("CalamityEntropy/Assets/Particles/LargeBloom", new Vector2(1, 1), Main.rand.NextFloat(-10, 10), 4f * 0.16f, 3f * 0.16f, 20);

                Vector2 BurstFXDirection = new Vector2(0, 6 * 0.16f).RotatedBy(MathHelper.PiOver4);
                for (int i = 0; i < 16; i++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (BurstFXDirection) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.6f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
                }
                for (int k = 0; k < 25; k++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(30 * 0.16f, 30 * 0.16f), BurstFXDirection * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.03f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
                }
                for (int i = 0; i < 16; i++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (-BurstFXDirection) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.6f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
                }
                for (int k = 0; k < 25; k++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(30, 30), -BurstFXDirection * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.03f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
                }
                Vector2 BurstFXDirection2 = new Vector2(6 * 0.16f, 0).RotatedBy(MathHelper.PiOver4);
                for (int i = 0; i < 16; i++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (BurstFXDirection2) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.6f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
                }
                for (int k = 0; k < 25; k++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(30, 30), BurstFXDirection2 * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.03f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
                }
                for (int i = 0; i < 16; i++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (-BurstFXDirection2) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.6f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
                }
                for (int k = 0; k < 25; k++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(30, 30), -BurstFXDirection2 * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.03f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
                }

                for (int i = 0; i < 10; i++) {
                    var randomColor = Main.rand.Next(4) switch {
                        0 => Color.Red,
                        1 => Color.MediumTurquoise,
                        2 => Color.Orange,
                        _ => Color.LawnGreen,
                    };

                    PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, randomColor * 0.7f, 0f).Configure("CalamityEntropy/Assets/Particles/FlameExplosion", new Vector2(1f, 1f), Main.rand.NextFloat(-20, 20), 0f, (4f - i * 0.28f) * 0.12f, 50);
                }
            }

            if (Projectile.timeLeft > 280) {
                SpawnDust();
                Vector2 top = Projectile.Center + CEUtils.randomPointInCircle(12);
                Vector2 sparkVelocity2 = Projectile.velocity * 0.8f;
                int sparkLifetime2 = Main.rand.Next(40, 60);
                float sparkScale2 = Main.rand.NextFloat(1f, 1.6f);
                Color sparkColor2 = Color.Lerp(Color.OrangeRed, Color.Firebrick, Main.rand.NextFloat(0, 1));
                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.timeLeft > 280) {
                Explode();
            }
        }

    }
}