using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class AzafureSwirlblade : ModItem, ICEChargeWeapon, IAzafureEnhancable
    {
        // 充能条 4 秒；原潜伏乘数 伤害0.75/弹速1.2 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(4f, 0.75f, 1.2f);

        public override void SetDefaults() {
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = Item.useTime = 46;
            Item.width = 74;
            Item.height = 70;
            Item.damage = 32;
            Item.crit = 3;
            Item.ArmorPenetration = 10;
            Item.UseSound = SoundID.Item1 with { Volume = 1.2f, Pitch = -0.32f };
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.shoot = ModContent.ProjectileType<AzafureSwirlbladeProj>();
            Item.shootSpeed = 45f;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            bool ult = CEChargeWeapon.TryConsume(player, Item);
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (ult && p >= 0 && p < Main.maxProjectiles) {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<ScorchingChakram>())
                .AddIngredient(ModContent.ItemType<FlamingSwirlblade>())
                .AddIngredient(ItemID.SoulofSight, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        public override bool RangedPrefix() {
            return true;
        }
    }
    public class AzafureSwirlbladeProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<AzafureSwirlblade>();
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 7;
        }
        public override float Radius => 140 * (Projectile.IsEmpowered() ? 1.18f : 1) * (player.AzafureEnhance() ? 1.2f : 1);
        public override int SpreadTime => (Projectile.IsEmpowered() ? 24 : 30) + (player.AzafureEnhance() ? 14 : 0);
        public override void AI() {
            base.AI();
            if (BladeScale >= 0.2f) {
                float particleRot = CEUtils.randomRot();
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), (Main.rand.NextBool() ? Color.Firebrick * 1.2f : Color.OrangeRed) * BladeScale, Main.rand.NextFloat(0.6f, 1f) * 0.04f * BladeScale * Projectile.scale).Configure(false, Main.rand.Next(12, 16), new Vector2(0.18f, 1f), false, false);
            }
            CEUtils.AddLight(Projectile.Center, new Color(255, 80, 80));
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 1) {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for (int i = 1; i < oldPos.Count; i++) {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int i = 0; i < posC.Count; i++) {
                    float p = ((float)(1 + i) / posC.Count);
                    Color clr = new Color(255, 170, 170) * 0.74f * p;
                    Main.spriteBatch.Draw(tex, posC[i] - Main.screenPosition, null, clr, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();

                for (int i = 0; i < posC.Count; i++) {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 24 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(255, 16, 16));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0) {
                Texture2D smear = CEExtraAssets.CircularSmearSmokey;
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;
                BaseSwirlblade.ApplyShader(new Color(255, 200, 180));
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * 50f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * -46f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.98f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * -38f, o, scale * 0.96f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread() {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.4f, 0.6f), Projectile.Center, volume: 0.86f);
            CEUtils.PlaySound("CogflyActive", Main.rand.NextFloat(1.32f, 1.4f), Projectile.Center);

            for (int i = 0; i < 8; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (i / 8f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.OrangeRed : Color.Firebrick, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);

            if (Main.myPlayer == Projectile.owner) {
                int flame = ModContent.ProjectileType<AzafureSwirlbladeMissile>();
                if (Projectile.IsEmpowered()) {
                    int totalCount = (player.AzafureEnhance() ? 10 : 8);
                    for (int i = 0; i < totalCount; i++) {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ((i / (float)totalCount) * MathHelper.TwoPi).ToRotationVector2() * 7, flame, (int)(Projectile.damage * 0.5f), 6, Projectile.owner);
                    }
                }
                else {
                    for (int i = 0; i < (player.AzafureEnhance() ? 3 : 2); i++) {
                        float dir = CEUtils.randomRot();
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir.ToRotationVector2() * 6, flame, (int)(Projectile.damage * 1f), 6, Projectile.owner);
                    }
                }
            }
        }
        public bool ShootSaw = true;
        public override void FlyBack() {
            if (TimeUtilSpread == 2)
                Projectile.velocity = CEUtils.randomRot().ToRotationVector2() * 34;
            float fm = float.Min(TimeUtilSpread, 26);
            if (Projectile.IsEmpowered()) {
                if (ShootSaw) {
                    Projectile.velocity *= 0.94f;
                }
                if (Projectile.localAI[2]++ > 12) {
                    if (ShootSaw) {
                        ShootSaw = false;

                        if (Main.myPlayer == Projectile.owner) {
                            int flame = ModContent.ProjectileType<AzafureSwirlbladeSaw>();
                            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1400);
                            if (target != null) {
                                float dir = (target.Center - Projectile.Center).ToRotation();
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir.ToRotationVector2() * 36, flame, (int)(Projectile.damage), 8, Projectile.owner, Radius);
                                Projectile.velocity += (Projectile.Center - target.Center).normalize() * 32;
                                nh = 8;
                                Counter = FlyTime + SpreadTime + 3;
                            }
                        }
                    }
                }
            }
            else {
                ShootSaw = false;
            }
            if (nh-- < 0 && !ShootSaw) {
                Projectile.velocity *= 1f - fm * 0.006f;
                Projectile.velocity += (player.MountedCenter - Projectile.Center).normalize() * fm * 0.47f;
                if (!ShootSaw && Projectile.Distance(player.MountedCenter) <= Projectile.velocity.Length() * 1.05f + 16) {
                    BackKill();
                    Projectile.velocity = (player.MountedCenter - Projectile.Center);
                }
            }
            if (nh > 0) {
                Projectile.velocity *= 0.9f;
            }
        }


        public int nh = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<MechanicalTrauma>(300);
            if (!target.boss) {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1f, 1.2f), target.Center, 8, 1);

            for (int i = 0; i < 10; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.OrangeRed : (Color.Firebrick * 1f), 0.04f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
        }
    }
    public class AzafureSwirlbladeSaw : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.localNPCHitCooldown = 6;
        }
        public override bool ShouldUpdatePosition() {
            return NoPosUpdate <= 0;
        }
        public override void AI() {
            if (Projectile.Entropy().FirstFrames) {
                SoundStyle ShootSound = new("CalamityEntropy/Assets/Sounds/SawShot", 2) { PitchVariance = 0f, Volume = 1 };
                SoundEngine.PlaySound(ShootSound, Projectile.Center);

                for (int i = 0; i < 32; i++) {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.3f, 1f) * 3.2f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(1.2f) * Main.rand.NextFloat(0.5f, 1) * 40;
                    dust.noGravity = false;
                    dust.color = Main.rand.NextBool() ? Color.Orange : Color.OrangeRed;
                    dust.fadeIn = 2f;
                }
            }
            if (NoPosUpdate > 0) {
                NoPosUpdate--;
            }
            else if (CD > 0) {
                CD--;
            }
            if (Projectile.timeLeft < 30)
                Projectile.Opacity -= 1 / 30f;
        }
        public int NoPosUpdate = 0;
        public int CD = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (CD <= 0) {
                NoPosUpdate = 14;
                CD = 12;

                for (int i = 0; i < 6; i++) {
                    float rot = 2;
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16), Color.OrangeRed, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16), Color.OrangeRed, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                }
            }

            CEUtils.PlaySound("slice", 1, target.Center);
            CEUtils.PlaySound("slice", 1, target.Center);
            target.AddBuff<MechanicalTrauma>(300);
            float scale = 1.5f;
            for (int i = 0; i < 12; i++) {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f;
                dust.velocity = CEUtils.randomPointInCircle(30);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.Orange : Color.OrangeRed;
                dust.fadeIn = 2f;
            }
            scale = 1.6f;
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.OrangeRed * 0.8f, scale * 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
        }

        public float BladeScale => 1;
        public float Radius => Projectile.ai[0];
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return new Circle(projHitbox.Center.ToVector2(), Radius * Projectile.scale * BladeScale).Intersects(targetHitbox);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D smear = CEExtraAssets.CircularSmearSmokey;
            float scale = Radius / 78f * Projectile.scale * BladeScale;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 o = smear.Size() * 0.5f;
            BaseSwirlblade.ApplyShader(new Color(255, 200, 180));
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * 50f, o, scale * 1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * -46f, o, scale * 1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.98f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 40, 30) * Projectile.Opacity * BladeScale, time * -38f, o, scale * 0.96f, SpriteEffects.None, 0);
            Main.spriteBatch.UseAdditiveClamp();
            for (float i = 0; i <= 1f; i += 0.2f) {
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition - Projectile.velocity * i * 4, null, new Color(255, 80, 60) * (1.2f - i) * Projectile.Opacity * BladeScale * 0.64f, time * -36f, o, scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? CanHitNPC(NPC target) {
            return Projectile.Opacity > 0.6f ? null : false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
    public class AzafureSwirlbladeMissile : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, true, 1);
            Projectile.width = Projectile.height = 12;
            Projectile.MaxUpdates = 4;
        }
        public override bool? CanHitNPC(NPC target) {
            return Projectile.ai[0] > 18 ? null : false;
        }
        public override void AI() {
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1600);
            int counter = (int)(Projectile.ai[0]++);
            if (target != null) {
                if (Projectile.ai[2] < 1)
                    Projectile.ai[2] += 0.005f;
                Projectile.rotation = Projectile.velocity.ToRotation();

                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedByRandom(0.3f) * -8, Color.Orange, 0.3f).Configure(0.3f, 16, Main.rand.NextFloat(-0.006f, 0.006f), true);
                for (int i = 0; i < 2; i++) {
                    var smoke = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * 3, CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.02f, 0.04f));
                    smoke.timeleftmax = 26;
                    smoke.Lifetime = 26;
                    smoke.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
                }
                if (counter > 30) {
                    Projectile.velocity *= 1f - Projectile.ai[2] * 0.18f;
                    Projectile.velocity += (target.Center - Projectile.Center).normalize() * Projectile.ai[2] * 1.8f;
                }
            }
            else {
                Projectile.ai[2] = 0;
                for (int i = 0; i < 2; i++) {
                    var smoke = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * 3, CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.02f, 0.04f));
                    smoke.timeleftmax = 26;
                    smoke.Lifetime = 26;
                    smoke.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
                }
                Projectile.ai[0] = 50;
                Projectile.velocity += new Vector2(0, 0.1f);
                Projectile.velocity *= 0.99f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<MechanicalTrauma>(300);
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("explosion1", Main.rand.NextFloat(1.5f, 1.8f), Projectile.Center, 6, 0.5f);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.2f, 0.04f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.04f, 0.15f, 9);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.2f, 0.04f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.04f, 0.1f, 7);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.2f, 1.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            if (Projectile.owner == Main.myPlayer) {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage / 2, 160, Projectile.DamageType);
            }
            for (int i = 0; i < 12; i++) {
                var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Firework_Yellow);
                d.scale = 0.8f;
                d.velocity = CEUtils.randomPointInCircle(14);
                d.position += d.velocity * 4;
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation - MathHelper.PiOver2, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
