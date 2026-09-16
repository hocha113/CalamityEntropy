using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Silence : ModItem, ICEChargeWeapon
    {
        // 充能条 6 秒；原潜伏乘数 伤害0.5/弹速1.2/击退0 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(6f, 0.5f, 1.2f, 0f);

        public override void SetDefaults() {
            Item.width = 36;
            Item.height = 34;
            Item.damage = 1750;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.ArmorPenetration = 50;
            Item.knockBack = 5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(platinum: 2);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<SilenceProj>();
            Item.shootSpeed = 32f;
            Item.DamageType = DamageClass.Ranged;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
            // 蓄势就绪时换发特化弹（原 ModifyStatsExtra 潜伏换弹逻辑）
            if (CEChargeWeapon.IsReady(Item))
                type = ModContent.ProjectileType<SilenceStealth>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (CEChargeWeapon.TryConsume(player, Item)) {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (p >= 0 && p < Main.maxProjectiles) {
                    CEChargeWeapon.Empower(p);
                }
                return false;
            }
            return true;
        }
    }
    public class SilenceProj : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 100000;
        }
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.timeLeft = 120;
            Projectile.width = Projectile.height = 82;
            Projectile.MaxUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(NoPosUpdate);
            writer.Write(Projectile.timeLeft);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            NoPosUpdate = reader.ReadUInt16();
            int t = reader.ReadInt32();
            if (t > 10)
                Projectile.timeLeft = t;
        }
        public int NoPosUpdate = 0;
        public override bool? CanDamage() {
            return !Hitted;
        }
        public int target { get { return (int)Projectile.ai[1]; } set { Projectile.ai[1] = value; } }
        public override bool ShouldUpdatePosition() {
            return NoPosUpdate <= 0;
        }
        public bool Hitted { get { return Projectile.ai[0] > 0; } set { Projectile.ai[0] = value ? 1 : 0; } }
        public float nv = 0;
        public override void AI() {
            Player player = Projectile.GetOwner();
            int tf = player.itemTimeMax * Projectile.MaxUpdates;
            if (Projectile.localAI[2]++ <= tf) {
                player.SetHandRotWithDir(Projectile.velocity.ToRotation() + (-2f + 4f * CEUtils.Parabola(Projectile.localAI[2] / tf * 0.5f, 1)) * player.direction, player.direction);
            }
            Projectile.rotation += 0.2f;
            if (NoPosUpdate <= 0) {
                if (!Hitted) {
                    Color clr = new Color(120, 120, 255);
                    Vector2 vel = Projectile.velocity.RotatedBy(MathHelper.PiOver2).normalize();
                    //VelChangingSpark/CustomPulse是CalamityPorts,Configure对齐Calamity原构造不是统一五参
                    PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + Projectile.velocity.normalize() * 22 + vel * 20, vel * 1, clr * 0.95f, 0.2f).Configure(-Projectile.velocity.normalize() * 16, "CalamityEntropy/Assets/Particles/BloomCircle", 7, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.34f);
                    PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + Projectile.velocity.normalize() * 22 - vel * 20, -vel * 1, clr * 0.95f, 0.2f).Configure(-Projectile.velocity.normalize() * 16, "CalamityEntropy/Assets/Particles/BloomCircle", 7, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.34f);
                    PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + Projectile.velocity.normalize() * 22 + vel * 20, vel * 1, clr * 0.95f, 0.2f).Configure(-Projectile.velocity.normalize() * 8, "CalamityEntropy/Assets/Particles/BloomCircle", 7, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.34f);
                    PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + Projectile.velocity.normalize() * 22 - vel * 20, -vel * 1, clr * 0.95f, 0.2f).Configure(-Projectile.velocity.normalize() * 8, "CalamityEntropy/Assets/Particles/BloomCircle", 7, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.34f);
                }
                else {
                    if (Projectile.ai[1] < 0 || !target.ToNPC().active) {
                        Projectile.Kill();
                        return;
                    }
                    Projectile.velocity *= 0.95f;
                    Projectile.localAI[0]++;
                    if (Projectile.localAI[1]++ >= 70) {
                        if (Projectile.localAI[1] > 110) {
                            Projectile.velocity *= 0.92f;
                            Projectile.velocity += (target.ToNPC().Center - Projectile.Center).normalize() * 8f;
                            if (Projectile.Colliding(Projectile.Hitbox, target.ToNPC().Hitbox)) {
                                Projectile.Kill();
                            }
                        }
                        if (Projectile.ai[2] < 1) {
                            Projectile.ai[2] += nv;
                            nv += 0.003f;
                            nv *= 1.03f;
                            Vector2 lp1 = Vector2.Zero;
                            Vector2 lp2 = Vector2.Zero;
                            for (float i = 0; i <= 1; i += 0.05f) {
                                if (Projectile.ai[2] + i * 0.1f >= 1f)
                                    break;
                                Vector2 p1 = GetChainPoint(Projectile.Center + Projectile.velocity, target.ToNPC().Center, Projectile.ai[2] + i * 0.1f, 14, FLEX, 1f);
                                Vector2 p2 = GetChainPoint(Projectile.Center + Projectile.velocity, target.ToNPC().Center, Projectile.ai[2] + i * 0.1f, 14, FLEX, -1f);
                                if (i == 0)
                                    continue;
                                Vector2 v1 = (p1 - lp1).normalize() * -2;
                                Vector2 v2 = (p2 - lp2).normalize() * -2;

                                Dust dust = Dust.NewDustPerfect(p1, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                                dust.scale = Main.rand.NextFloat(1.2f, 1.6f);
                                dust.velocity = v1;
                                dust.noGravity = true;
                                dust.color = new Color(20, 20, 255);
                                dust.fadeIn = 2f;

                                dust = Dust.NewDustPerfect(p2, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                                dust.scale = Main.rand.NextFloat(1.2f, 1.6f);
                                dust.velocity = v2;
                                dust.noGravity = true;
                                dust.color = new Color(20, 20, 255);
                                dust.fadeIn = 2f;
                                lp1 = p1;
                                lp2 = p2;
                            }
                            if (Projectile.ai[2] >= 1) {
                                Projectile.ai[2] = 1;
                                NPC t = target.ToNPC();
                                Vector2 v = (Projectile.Center - t.Center).normalize().RotatedBy(MathHelper.PiOver4);
                                //黑色VoidSpark抬AfterPlayers想盖蓝光,Calamity GeneralDrawLayer没法1:1迁,被盖了再往前提层
                                PRTLoader.NewParticle<PRT_GlowSparkCal>(t.Center, v, Color.SkyBlue, 0.04f).Configure(false, 11, new Vector2(8, 2), true, false);
                                PRTLoader.NewParticle<PRT_GlowSparkCal>(t.Center, v.RotatedBy(MathHelper.PiOver2), Color.SkyBlue, 0.04f).Configure(false, 11, new Vector2(8, 2), true, false);
                                PRTLoader.NewParticle<PRT_VoidSparkCal>(t.Center, v, Color.Black, 0.36f).Configure(false, 9, renderLayer: PRTRenderLayer.AfterPlayers);
                                PRTLoader.NewParticle<PRT_VoidSparkCal>(t.Center, v.RotatedBy(MathHelper.PiOver2), Color.Black, 0.36f).Configure(false, 9, renderLayer: PRTRenderLayer.AfterPlayers);
                                CEUtils.PlaySound("AugerHit", Main.rand.NextFloat(1f, 1.3f), Projectile.Center);
                                CEUtils.PlaySound("AugerHit", Main.rand.NextFloat(1f, 1.3f), Projectile.Center);
                                CEUtils.PlaySound("AugerHit", Main.rand.NextFloat(1f, 1.3f), Projectile.Center);
                            }
                        }
                    }
                }
            }
            else {
                NoPosUpdate--;
            }
        }
        public override void OnKill(int timeLeft) {
            if (Hitted) {
                float p = Main.rand.NextFloat(0.7f, 1f);
                CEUtils.PlaySound("CarverHit", p, Projectile.Center, 8, 0.6f);
                Vector2 v = Projectile.rotation.ToRotationVector2();
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4) {
                    //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, v.RotatedBy(i), Color.SkyBlue, 0.03f).Configure(false, 16, new Vector2(24, 2), true, false);
                    PRTLoader.NewParticle<PRT_VoidSparkCal>(Projectile.Center, v.RotatedBy(i), Color.Black, 0.4f).Configure(false, 16, renderLayer: PRTRenderLayer.AfterPlayers);
                }
                float scale = 5.4f;

                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Blue * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 22);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.03f, 19);

                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, 200, Projectile.DamageType);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 240, 11, 600, 12);
            if (!Hitted) {
                Projectile.timeLeft = 600;
                Hitted = true;
                this.target = target.whoAmI;
                NoPosUpdate = 8;
                CEUtils.SyncProj(Projectile.whoAmI);
                CEUtils.PlaySound("slice", 1, target.Center);
                CEUtils.PlaySound("slice", 1, target.Center);
                CEUtils.PlaySound("slice", 1, target.Center);
                for (int i = 0; i < 6; i++) {
                    float rot = 2;
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * 20 * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.2f) * Main.rand.NextFloat(4, 16), Color.SkyBlue, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * 20 * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.2f) * Main.rand.NextFloat(4, 16), Color.SkyBlue, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                }
                for (int i = 0; i < 16; i++) {
                    Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(2.2f, 3.2f);
                    dust.velocity = Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.4f, 1f);
                    dust.noGravity = true;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }

            }
        }
        public static float FLEX = 120;
        public override bool PreDraw(ref Color lightColor) {
            if (Hitted) {
                if (target.ToNPC().active)
                    DrawChain(Projectile.Center, target.ToNPC().Center, Projectile.ai[2], 14, FLEX);
            }
            Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, Projectile.GetTexture().Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            Main.graphics.GraphicsDevice.Textures[0] = CEExtraAssets.MegaStreakBacking2c;
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public static Vector2 GetChainPoint(Vector2 start, Vector2 end, float rate, float width, float flex = 60, float mul = 1) {
            if (start == end)
                return start;
            return Vector2.Lerp(start, end, rate) + (end - start).normalize().RotatedBy(MathHelper.PiOver2) * width * mul * (float)(Math.Cos(-MathHelper.PiOver2 + ((CEUtils.getDistance(start, end) * rate) / flex) * MathHelper.TwoPi));
        }
        public static void DrawChain(Vector2 start, Vector2 end, float rate, float width, float flex = 60) {
            if (rate <= 0)
                return;
            Main.spriteBatch.ExitShaderRegion();
            if (start == end) return;
            rate = MathHelper.Clamp(rate, 0, 1);
            List<ColoredVertex> ve1a = new List<ColoredVertex>();
            List<ColoredVertex> ve1b = new List<ColoredVertex>();
            List<ColoredVertex> ve2a = new List<ColoredVertex>();
            List<ColoredVertex> ve2b = new List<ColoredVertex>();
            Color color1 = new Color(190, 150, 255);
            Color color2 = new Color(0, 0, 0);
            float w = 8f;
            float wInner = 0.46f;
            Vector2 lastPos1 = Vector2.Zero;
            Vector2 lastPos2 = Vector2.Zero;
            for (float i = 0; i <= rate; i += 0.01f) {
                Vector2 p1 = GetChainPoint(start, end, i, width, flex, 1);
                Vector2 p2 = GetChainPoint(start, end, i, width, flex, -1);
                Vector2 o1 = i == 0 ? Vector2.Zero : (p1 - lastPos1).normalize().RotatedBy(-MathHelper.PiOver2);
                Vector2 o2 = i == 0 ? Vector2.Zero : (p2 - lastPos2).normalize().RotatedBy(-MathHelper.PiOver2);

                ve1a.Add(new ColoredVertex(p1 - o1 * w - Main.screenPosition, new Vector3(i * 8, 1, 1), color1));
                ve1a.Add(new ColoredVertex(p1 + o1 * w - Main.screenPosition, new Vector3(i * 8, 0, 1), color1));

                ve1b.Add(new ColoredVertex(p1 - o1 * w * wInner - Main.screenPosition, new Vector3(i * 8, 1, 1), color2));
                ve1b.Add(new ColoredVertex(p1 + o1 * w * wInner - Main.screenPosition, new Vector3(i * 8, 0, 1), color2));

                ve2a.Add(new ColoredVertex(p2 - o2 * w - Main.screenPosition, new Vector3(i * 8, 1, 1), color1));
                ve2a.Add(new ColoredVertex(p2 + o2 * w - Main.screenPosition, new Vector3(i * 8, 0, 1), color1));

                ve2b.Add(new ColoredVertex(p2 - o2 * w * wInner - Main.screenPosition, new Vector3(i * 8, 1, 1), color2));
                ve2b.Add(new ColoredVertex(p2 + o2 * w * wInner - Main.screenPosition, new Vector3(i * 8, 0, 1), color2));
                lastPos1 = p1;
                lastPos2 = p2;
            }
            var gd = Main.graphics.GraphicsDevice;
            Texture2D tx = CEExtraAssets.MegaStreakBacking2c;
            gd.Textures[0] = tx;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve1a.ToArray(), 0, ve1a.Count - 2);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2a.ToArray(), 0, ve2a.Count - 2);
            Main.spriteBatch.End();
            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve1b.ToArray(), 0, ve1b.Count - 2);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2b.ToArray(), 0, ve2b.Count - 2);
            Main.spriteBatch.ExitShaderRegion();
        }
    }
    public class SilenceStealth : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 100000;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/SilenceProj";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.timeLeft = 120;
            Projectile.width = Projectile.height = 82;
            Projectile.MaxUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(NoPosUpdate);
            writer.Write(Projectile.timeLeft);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            NoPosUpdate = reader.ReadUInt16();
            int t = reader.ReadInt32();
            if (t > 10)
                Projectile.timeLeft = t;
        }
        public int NoPosUpdate = 0;
        public override bool? CanDamage() {
            if (Hitted && areaSize == 0)
                return false;
            return null;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (Hitted) {
                modifiers.SourceDamage *= 0.05f;
                modifiers.ArmorPenetration += 500;
            }
        }
        public int target { get { return (int)Projectile.ai[1]; } set { Projectile.ai[1] = value; } }
        public override bool ShouldUpdatePosition() {
            return NoPosUpdate <= 0;
        }
        public bool Hitted { get { return Projectile.ai[0] > 0; } set { Projectile.ai[0] = value ? 1 : 0; } }
        public float nv = 0;
        public float areaSize = 0;
        public float areaAlpha = 0;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float w = Projectile.width + areaSize * 4.6f;
            return Projectile.Center.getRectCentered(w, w).Intersects(targetHitbox);
        }
        public override void AI() {
            Player player = Projectile.GetOwner();
            int tf = player.itemTimeMax * Projectile.MaxUpdates;
            if (Projectile.localAI[2]++ <= tf) {
                player.SetHandRotWithDir(Projectile.velocity.ToRotation() + (-2f + 4f * CEUtils.Parabola(Projectile.localAI[2] / tf * 0.5f, 1)) * player.direction, player.direction);
            }
            Projectile.rotation += 0.2f;
            if (NoPosUpdate <= 0) {
                if (!Hitted) {
                    Color clr = new Color(180, 60, 255);
                    Vector2 vel = Projectile.velocity.RotatedBy(MathHelper.PiOver2).normalize();
                    PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + Projectile.velocity.normalize() * 22 + vel * 20, vel * 1, clr * 0.95f, 0.2f).Configure(-Projectile.velocity.normalize() * 16, "CalamityEntropy/Assets/Particles/BloomCircle", 10, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.34f);
                    PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + Projectile.velocity.normalize() * 22 - vel * 20, -vel * 1, clr * 0.95f, 0.2f).Configure(-Projectile.velocity.normalize() * 16, "CalamityEntropy/Assets/Particles/BloomCircle", 10, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.34f);
                }
                else {
                    if (Projectile.ai[1] < 0 || !target.ToNPC().active) {
                        Projectile.Kill();
                        return;
                    }
                    Projectile.velocity *= 0.95f;
                    Projectile.localAI[0]++;
                    if (Projectile.localAI[1]++ >= 70) {
                        if (Projectile.localAI[1] == 110)
                            CEUtils.PlaySound("VortexSpawn", 1, Projectile.Center);
                        if (Projectile.localAI[1] > 110) {
                            Projectile.velocity = (target.ToNPC().Center - Projectile.Center) * 0.08f;
                            if (Projectile.timeLeft > 14) {
                                areaSize = float.Lerp(areaSize, 100 * Projectile.scale, 0.023f);
                                areaAlpha = float.Lerp(areaAlpha, 1f, 0.04f);
                            }
                            else {
                                areaSize *= 0.9f;
                                areaAlpha *= 0.9f;
                            }
                        }
                        if (Projectile.ai[2] < 1) {
                            Projectile.ai[2] += nv;
                            nv += 0.003f;
                            nv *= 1.03f;
                            Vector2 lp1 = Vector2.Zero;
                            Vector2 lp2 = Vector2.Zero;
                            for (float i = 0; i <= 1; i += 0.05f) {
                                if (Projectile.ai[2] + i * 0.1f >= 1f)
                                    break;
                                Vector2 p1 = GetChainPoint(Projectile.Center + Projectile.velocity, target.ToNPC().Center, Projectile.ai[2] + i * 0.1f, 14, FLEX, 1f);
                                Vector2 p2 = GetChainPoint(Projectile.Center + Projectile.velocity, target.ToNPC().Center, Projectile.ai[2] + i * 0.1f, 14, FLEX, -1f);
                                if (i == 0)
                                    continue;
                                Vector2 v1 = (p1 - lp1).normalize() * -2;
                                Vector2 v2 = (p2 - lp2).normalize() * -2;

                                Dust dust = Dust.NewDustPerfect(p1, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                                dust.scale = Main.rand.NextFloat(1.2f, 1.6f);
                                dust.velocity = v1;
                                dust.noGravity = true;
                                dust.color = Color.LightBlue;
                                dust.fadeIn = 2f;

                                dust = Dust.NewDustPerfect(p2, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                                dust.scale = Main.rand.NextFloat(1.2f, 1.6f);
                                dust.velocity = v2;
                                dust.noGravity = true;
                                dust.color = Color.LightBlue;
                                dust.fadeIn = 2f;
                                lp1 = p1;
                                lp2 = p2;
                            }
                            if (Projectile.ai[2] >= 1) {
                                Projectile.ai[2] = 1;
                                NPC t = target.ToNPC();
                                Vector2 v = (Projectile.Center - t.Center).normalize().RotatedBy(MathHelper.PiOver4);
                                PRTLoader.NewParticle<PRT_GlowSparkCal>(t.Center, v, Color.SkyBlue, 0.04f).Configure(false, 11, new Vector2(8, 2), true, false);
                                PRTLoader.NewParticle<PRT_GlowSparkCal>(t.Center, v.RotatedBy(MathHelper.PiOver2), Color.SkyBlue, 0.04f).Configure(false, 11, new Vector2(8, 2), true, false);
                                PRTLoader.NewParticle<PRT_VoidSparkCal>(t.Center, v, Color.Black, 0.36f).Configure(false, 9, renderLayer: PRTRenderLayer.AfterPlayers);
                                PRTLoader.NewParticle<PRT_VoidSparkCal>(t.Center, v.RotatedBy(MathHelper.PiOver2), Color.Black, 0.36f).Configure(false, 9, renderLayer: PRTRenderLayer.AfterPlayers);
                                CEUtils.PlaySound("AugerHit", Main.rand.NextFloat(1f, 1.3f), Projectile.Center);
                                CEUtils.PlaySound("AugerHit", Main.rand.NextFloat(1f, 1.3f), Projectile.Center);
                                CEUtils.PlaySound("AugerHit", Main.rand.NextFloat(1f, 1.3f), Projectile.Center);
                            }
                        }
                    }
                }
            }
            else {
                NoPosUpdate--;
            }
        }
        public override void OnKill(int timeLeft) {
            if (Hitted) {
                float p = Main.rand.NextFloat(0.7f, 1f);
                CEUtils.PlaySound("CarverHit", p, Projectile.Center, 8, 0.6f);
                Vector2 v = Projectile.rotation.ToRotationVector2();
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4) {
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, v.RotatedBy(i), Color.SkyBlue, 0.03f).Configure(false, 16, new Vector2(24, 2), true, false);
                    PRTLoader.NewParticle<PRT_VoidSparkCal>(Projectile.Center, v.RotatedBy(i), Color.Black, 0.4f).Configure(false, 16, renderLayer: PRTRenderLayer.AfterPlayers);
                }
                float scale = 5.4f;

                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Blue * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 22);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.03f, 19);

                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, 200, Projectile.DamageType);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 240, 11, 600, 12);
            if (!Hitted) {
                Projectile.timeLeft = 400;
                Hitted = true;
                this.target = target.whoAmI;
                NoPosUpdate = 8;
                CEUtils.SyncProj(Projectile.whoAmI);
                CEUtils.PlaySound("slice", 1, target.Center);
                CEUtils.PlaySound("slice", 1, target.Center);
                CEUtils.PlaySound("slice", 1, target.Center);
                for (int i = 0; i < 7; i++) {
                    float rot = 2;
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * 20 * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.2f) * Main.rand.NextFloat(4, 16), Color.SkyBlue, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * 20 * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.2f) * Main.rand.NextFloat(4, 16), Color.SkyBlue, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                }
                for (int i = 0; i < 16; i++) {
                    Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(2.2f, 3.2f);
                    dust.velocity = Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.4f, 1f);
                    dust.noGravity = true;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }
            }
            else {
                SoundStyle burn = new SoundStyle("CalamityEntropy/Assets/Sounds/steam") { PitchVariance = 0.2f };
                SoundEngine.PlaySound(burn with { Volume = 0.28f, Pitch = 0.5f }, target.Center);
                for (int i = 0; i < 3; i++) {
                    Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(2f, 2.6f);
                    dust.velocity = (new Vector2(24, 24).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                    dust.noGravity = false;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }
            }
        }
        public static float FLEX = 120;
        public override bool PreDraw(ref Color lightColor) {
            if (Hitted) {
                if (target.ToNPC().active)
                    DrawChain(Projectile.Center, target.ToNPC().Center, Projectile.ai[2], 14, FLEX);
            }
            DrawVortex();
            Main.graphics.GraphicsDevice.Textures[0] = CEExtraAssets.MegaStreakBacking2c;
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, Projectile.GetTexture().Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        public void DrawVortex() {
            float aScale = 0.06f;
            Main.spriteBatch.End();
            Effect effect = CEEffectAssets.Vortex;
            effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
            effect.Parameters["Strength"].SetValue(16);
            effect.Parameters["AspectRatio"].SetValue(1);
            effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
            float fadeOutDistance = 0.06f;
            float fadeOutWidth = 0.3f;
            effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
            effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
            effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(CEExtraAssets.VoronoiShapes, Projectile.Center - Main.screenPosition, null, new Color(126, 126, 255) * areaAlpha, Main.GlobalTimeWrappedHourly * 15, CEExtraAssets.VoronoiShapes.Size() / 2f, 0.17f * Projectile.scale * areaSize * aScale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(CEExtraAssets.VoronoiShapes, Projectile.Center - Main.screenPosition, null, new Color(126, 126, 255) * areaAlpha, Main.GlobalTimeWrappedHourly * 12, CEExtraAssets.VoronoiShapes.Size() / 2f, 0.17f * Projectile.scale * areaSize * aScale, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            Texture2D gt = CEExtraAssets.lightball;
            CEUtils.DrawGlow(Projectile.Center, Color.Black * areaAlpha, 0.5f * areaSize * aScale, false, gt);
            CEUtils.DrawGlow(Projectile.Center, Color.Black * areaAlpha, 0.5f * areaSize * aScale, false, gt);
            DrawRing(Projectile.Center - Main.screenPosition, CEExtraAssets.StreakGoop, new Vector2(68, 68) * areaSize * aScale, new Vector2(20, 20) * areaSize * aScale, new Color(140, 120, 200) * areaAlpha, BlendState.Additive, Main.GlobalTimeWrappedHourly * -5);
            DrawRing(Projectile.Center - Main.screenPosition, CEExtraAssets.StreakGoop, new Vector2(74, 74) * areaSize * aScale, new Vector2(14, 14) * areaSize * aScale, new Color(140, 120, 200) * areaAlpha, BlendState.Additive, Main.GlobalTimeWrappedHourly * -3);
        }
        public void DrawRing(Vector2 position, Texture2D trail, Vector2 scaleOutside, Vector2 scaleInside, Color color, BlendState blend, float r = 0, bool? drawUpside = null) {
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<Vector2> points1 = new List<Vector2>();
            List<Vector2> points2 = new List<Vector2>();
            for (float i = 0; i <= 1; i += 0.01f) {
                Vector2 rv = (i * MathHelper.TwoPi).ToRotationVector2();
                Vector2 p = rv * scaleOutside;
                if (drawUpside.HasValue) {
                    if (drawUpside.Value) {
                        if (i == 0)
                            i = 0.5f;
                    }
                    else {
                        if (i > 0.5f)
                            break;
                    }
                }
                rv = (i * MathHelper.TwoPi).ToRotationVector2();
                p = rv * scaleOutside;
                points1.Add(p);
                points2.Add((i * MathHelper.TwoPi).ToRotationVector2() * scaleInside);
            }
            for (int i = 0; i < points1.Count; i++) {
                ve.Add(new ColoredVertex(position + points1[i], color, new Vector3(i / 50f + r, 0, 1)));
                ve.Add(new ColoredVertex(position + points2[i], color, new Vector3(i / 50f + r, 1, 1)));
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.Textures[0] = trail;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            Main.spriteBatch.ExitShaderRegion();
        }
        public static Vector2 GetChainPoint(Vector2 start, Vector2 end, float rate, float width, float flex = 60, float mul = 1) {
            if (start == end)
                return start;
            return Vector2.Lerp(start, end, rate) + (end - start).normalize().RotatedBy(MathHelper.PiOver2) * width * mul * (float)(Math.Cos(-MathHelper.PiOver2 + ((CEUtils.getDistance(start, end) * rate) / flex) * MathHelper.TwoPi));
        }
        public static void DrawChain(Vector2 start, Vector2 end, float rate, float width, float flex = 60) {
            if (rate <= 0)
                return;
            Main.spriteBatch.ExitShaderRegion();
            if (start == end) return;
            rate = MathHelper.Clamp(rate, 0, 1);
            List<ColoredVertex> ve1a = new List<ColoredVertex>();
            List<ColoredVertex> ve1b = new List<ColoredVertex>();
            List<ColoredVertex> ve2a = new List<ColoredVertex>();
            List<ColoredVertex> ve2b = new List<ColoredVertex>();
            Color color1 = new Color(190, 150, 255);
            Color color2 = new Color(0, 0, 0);
            float w = 8f;
            float wInner = 0.46f;
            Vector2 lastPos1 = Vector2.Zero;
            Vector2 lastPos2 = Vector2.Zero;
            for (float i = 0; i <= rate; i += 0.01f) {
                Vector2 p1 = GetChainPoint(start, end, i, width, flex, 1);
                Vector2 p2 = GetChainPoint(start, end, i, width, flex, -1);
                Vector2 o1 = i == 0 ? Vector2.Zero : (p1 - lastPos1).normalize().RotatedBy(-MathHelper.PiOver2);
                Vector2 o2 = i == 0 ? Vector2.Zero : (p2 - lastPos2).normalize().RotatedBy(-MathHelper.PiOver2);

                ve1a.Add(new ColoredVertex(p1 - o1 * w - Main.screenPosition, new Vector3(i * 8, 1, 1), color1));
                ve1a.Add(new ColoredVertex(p1 + o1 * w - Main.screenPosition, new Vector3(i * 8, 0, 1), color1));

                ve1b.Add(new ColoredVertex(p1 - o1 * w * wInner - Main.screenPosition, new Vector3(i * 8, 1, 1), color2));
                ve1b.Add(new ColoredVertex(p1 + o1 * w * wInner - Main.screenPosition, new Vector3(i * 8, 0, 1), color2));

                ve2a.Add(new ColoredVertex(p2 - o2 * w - Main.screenPosition, new Vector3(i * 8, 1, 1), color1));
                ve2a.Add(new ColoredVertex(p2 + o2 * w - Main.screenPosition, new Vector3(i * 8, 0, 1), color1));

                ve2b.Add(new ColoredVertex(p2 - o2 * w * wInner - Main.screenPosition, new Vector3(i * 8, 1, 1), color2));
                ve2b.Add(new ColoredVertex(p2 + o2 * w * wInner - Main.screenPosition, new Vector3(i * 8, 0, 1), color2));
                lastPos1 = p1;
                lastPos2 = p2;
            }
            var gd = Main.graphics.GraphicsDevice;
            Texture2D tx = CEExtraAssets.MegaStreakBacking2c;
            gd.Textures[0] = tx;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve1a.ToArray(), 0, ve1a.Count - 2);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2a.ToArray(), 0, ve2a.Count - 2);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve1b.ToArray(), 0, ve1b.Count - 2);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2b.ToArray(), 0, ve2b.Count - 2);
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
