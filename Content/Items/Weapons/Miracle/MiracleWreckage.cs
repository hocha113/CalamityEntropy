using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Miracle
{
    public class MiracleWreckage : ModItem
    {
        public const int MaxPlugged = 6;
        public override void SetDefaults()
        {
            Item.damage = 1600;
            Item.crit = 50;
            Item.DamageType = ModContent.GetInstance<MeleeDamageClass>();
            Item.width = 48;
            Item.height = 60;
            Item.useTime = 46;
            Item.useAnimation = 46;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ModContent.RarityType<ShiningViolet>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MiracleWreckageHeldAnm>();
            Item.shootSpeed = 16f;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int heldType = ModContent.ProjectileType<MiracleWreckageHeld>();
            if (player.ownedProjectileCounts[heldType] > 0)
                return false;
            if (player.altFunctionUse == 2)
            {
                CEUtils.PlaySound("flamethrower start", 1.6f, position);
                Projectile.NewProjectile(source, position, velocity, heldType, damage, knockback, player.whoAmI);
                return false;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DevilsDevastation))
            {
                CreateRecipe().AddIngredient(CEID.Item_DevilsDevastation).
                AddIngredient<FadingRunestone>(2).
                AddTile<VoidWellTile>().
                Register();
                return;
            }
            CreateRecipe().AddIngredient<FadingRunestone>(1)
                .AddTile<VoidWellTile>().
                Register();
        }
    }
    public class MiracleWreckageHeldAnm : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Miracle/MiracleWreckage";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 90;
            Projectile.timeLeft = 4;
            Projectile.light = 1;
            Projectile.MaxUpdates = 3;
        }
        public float rot = 0;
        public float rotVel = 0;
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            Projectile.timeLeft = 4;
            Player player = Projectile.GetOwner();
            float speed = player.GetTotalAttackSpeed(DamageClass.Melee);
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 1)
            {
                rot = -0.7f;
                rotVel = -0.12f;
                Projectile.ai[1] = Projectile.velocity.X > 0 ? 1 : -1;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + rot * Projectile.ai[1];
            player.SetHandRotWithDir(Projectile.rotation, Math.Sign(Projectile.ai[1]));
            player.heldProj = Projectile.whoAmI;
            Projectile.Center = player.GetDrawCenter();
            player.Entropy().MouseWorldListener = true;
            Projectile.velocity = (player.Entropy().MouseWorld - Projectile.Center).normalize() * Projectile.velocity.Length();
            rot += rotVel * speed;
            rotVel *= (float)Math.Pow(0.94f, speed);

            if (Projectile.ai[0] * speed > 60)
            {
                if (Projectile.localAI[1] == 0)
                {
                    Projectile.localAI[1] = 1;
                    rotVel = 0.3f;
                }
            }
            if (Projectile.ai[0] * speed > 72)
            {
                if (Projectile.ai[2] == 0)
                {
                    Projectile.ai[2] = 1;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<MiracleWreckageThrow>(), Projectile.damage, 6, Projectile.owner);
                    }
                    rot = 0;
                    rotVel = 0;
                }
            }
            if (Projectile.ai[0] * speed > 88)
            {
                if (Projectile.ai[2] <= 1)
                {
                    Projectile.ai[2] = 2;
                }
            }
            if (Projectile.ai[0] * speed > 92)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<MiracleWreckageThrow>(), Projectile.damage, 6, Projectile.owner);
                }
                Projectile.Kill();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = new Vector2(0, tex.Height);
            float rot = Projectile.rotation + MathHelper.PiOver4;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, rot, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
    public class MiracleWreckageThrow : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Miracle/MiracleWreckage";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 90;
            Projectile.timeLeft = 120;
            Projectile.light = 1;
            Projectile.MaxUpdates = 3;
        }
        public int Hit = 0; //1 for npc  2 for tile
        public Vector2 offset = Vector2.Zero;
        public int target = -1;
        public uint hitTime = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Hit);
            writer.WriteVector2(offset);
            writer.Write(target);
            writer.Write(hitTime);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Hit = reader.ReadInt32();
            offset = reader.ReadVector2();
            target = reader.ReadInt32();
            hitTime = reader.ReadUInt32();
        }
        public override bool? CanHitNPC(NPC target)
        {
            return Hit == 0 ? null : false;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.timeLeft = 30;
            Projectile.velocity = oldVelocity;
            if (Hit == 0)
            {
                hitTime = Main.GameUpdateCount;
                Projectile.velocity *= 0.1f;
                Hit = 2;
                HitEffect(Projectile.Center + Projectile.rotation.ToRotationVector2() * 90);
            }
            return false;
        }
        public void Update()
        {
            if (Main.myPlayer == Projectile.owner && Main.netMode == NetmodeID.MultiplayerClient)
                CEUtils.SyncProj(Projectile.whoAmI);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Hit == 0)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<MiracleExplosion>(), Projectile.damage, 0, Projectile.owner);
                CEUtils.PlaySound("DemonSwordImpact2", Main.rand.NextFloat(0.9f, 1.2f), target.Center);
                hitTime = Main.GameUpdateCount;
                Projectile.timeLeft = 50 * 60;
                uint last = uint.MaxValue;
                Projectile lastProj = null;
                int amount = 0;
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.type == Projectile.type && p.whoAmI != Projectile.whoAmI && p.owner == Projectile.owner)
                    {
                        if (p.ModProjectile is MiracleWreckageThrow mw)
                        {
                            if (mw.Hit == 1 && mw.target == target.whoAmI)
                            {
                                amount++;
                                if (mw.hitTime < last)
                                {
                                    lastProj = p;
                                    last = mw.hitTime;
                                }
                            }
                        }
                    }
                }
                if (amount > MiracleWreckage.MaxPlugged)
                {
                    if (lastProj != null)
                    {
                        lastProj.rotation = Projectile.velocity.ToRotation();
                        ((MiracleWreckageThrow)lastProj.ModProjectile).PopOut();
                    }
                }
                Hit = 1;
                this.target = target.whoAmI;
                offset = Projectile.Center - target.Center;
                HitEffect(Projectile.Center + Projectile.rotation.ToRotationVector2() * 90);
            }
            Update();
        }
        public void PopOut()
        {
            for (int i = 0; i < 16; i++)
            {
                Color clr = Main.rand.NextBool() ? new Color(200, 200, 255) : new Color(190, 140, 255);
                //CustomPulse/CustomSpark贴图路径Configure现传,Texture属性只挂占位白图
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedByRandom(0.12f) * Main.rand.NextFloat(32, 64), clr, Main.rand.NextFloat(0.5f, 1) * 0.08f).Configure(false, 16, new Vector2(0.16f, 1));
            }
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedByRandom(0.5f) * 3, ModContent.ProjectileType<MiracleWreckagePopOut>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            Projectile.Kill();
        }

        public void HitEffect(Vector2 position)
        {
            for (int i = 0; i < 16; i++)
            {
                Color clr = Main.rand.NextBool() ? new Color(200, 200, 255) : new Color(190, 140, 255);
                //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
                PRTLoader.NewParticle<PRT_GlowSparkCal>(position, Projectile.rotation.ToRotationVector2().RotatedByRandom(0.3f) * -1 * Main.rand.NextFloat(12, 36), clr, Main.rand.NextFloat(0.5f, 1) * 0.08f).Configure(false, 16, new Vector2(0.16f, 1));
            }
        }
        public override bool ShouldUpdatePosition()
        {
            return Hit != 1;
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                CEUtils.PlaySound("DemonSwordSwing1", Main.rand.NextFloat(1.5f, 1.9f), Projectile.Center);
            }
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Hit == 1)
            {
                if (!target.ToNPC().active)
                { PopOut(); return; }
                if (Projectile.timeLeft < 30)
                {
                    StrokeAlpha = float.Lerp(StrokeAlpha, 0, 0.01f);
                    Projectile.Opacity -= 1 / 30f;
                    Projectile.velocity *= 0.95f;
                }
                else
                    StrokeAlpha = float.Lerp(StrokeAlpha, 1, 0.3f);
                Projectile.Center = target.ToNPC().Center + offset;
            }
            else if (Hit == 2)
            {
                StrokeAlpha *= 0.99f;
                Projectile.Opacity -= 1 / 30f;
            }
            else
            {
                Color clr = Main.rand.NextBool() ? new Color(200, 200, 255) : new Color(190, 140, 255);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center - Projectile.rotation.ToRotationVector2() * 90 + Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-16, 16), Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(12, 36), clr, Main.rand.NextFloat(0.5f, 1) * 0.02f).Configure(false, 16, new Vector2(0.16f, 1) * Projectile.Opacity * 1f);
                for (float j = 0; j < 1; j += 0.5f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        clr = Main.rand.NextBool() ? new Color(235, 40, 180) : new Color(255, 40, 180);
                        Vector2 offset = Projectile.velocity * -j;
                        Vector2 safeVel = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                        Vector2 dustVel = safeVel.RotatedBy(MathHelper.ToRadians(70 * (i == 0 ? 1 : -1))) * 10;
                        Vector2 ofs2 = -dustVel * j * (Projectile.localAI[0] % Projectile.MaxUpdates);
                        if (true)
                        {
                            PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + ofs2 + offset + safeVel * 120, -dustVel, (clr) * 0.95f, 0.2f).Configure(-Projectile.velocity, "CalamityEntropy/Assets/Particles/BloomCircle", 5, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.22f);

                            PRTLoader.NewParticle<PRT_VelChangingSpark>(Projectile.Center + ofs2 + offset + safeVel * 120, -dustVel, clr * 0.65f, 0.2f).Configure(-Projectile.velocity, "CalamityEntropy/Assets/Particles/BloomCircle", 10, new Vector2(1.2f, 1f), true, false, 0, 1.0f, 0.22f);
                        }
                        float rot = Projectile.rotation + (MathHelper.TwoPi * i);
                        Vector2 vel = (Utils.MoveTowards(-Projectile.velocity, new Vector2(0, -130).RotatedBy(rot).RotatedBy(-1.3f * Projectile.direction), (Utils.GetLerpValue(5, 2, Projectile.velocity.Length(), true))));
                        if (i == 0)
                        {
                            Dust dust2 = Dust.NewDustPerfect(Projectile.Center + offset + new Vector2(0, -70).RotatedBy(rot), Main.rand.NextBool(4) ? 278 : 267);
                            dust2.noGravity = (dust2.type == 278 ? false : true);
                            dust2.scale = dust2.type == 278 ? 0.75f : 0.9f;
                            dust2.color = Main.rand.NextBool() ? Color.BlueViolet : clr;
                            dust2.velocity = (vel * 2).RotatedByRandom(0.4f);
                            dust2.position = Projectile.Center;
                        }
                    }
                }
                if (Projectile.timeLeft < 30)
                {
                    StrokeAlpha = float.Lerp(StrokeAlpha, 0, 0.01f);
                    Projectile.Opacity -= 1 / 30f;
                    Projectile.velocity *= 0.95f;
                }
                else
                {
                    StrokeAlpha = float.Lerp(StrokeAlpha, 1, 0.1f);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Vector2 origin = tex.Size() / 2f;
            float rotation = Projectile.rotation + MathHelper.PiOver4;
            for (float i = 0; i < 360; i += 60)
            {
                float rot = MathHelper.ToRadians(i) + Main.GlobalTimeWrappedHourly * 16;
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + CEUtils.randomPointInCircle(2) + rot.ToRotationVector2() * 4, null, Color.White * StrokeAlpha * Projectile.Opacity, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public float StrokeAlpha = 0;
    }
    public class MiracleExplosion : ModProjectile
    {
        public int endTime = 25;

        public ref float time => ref Projectile.ai[0];

        public override string Texture => "CalamityEntropy/Assets/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 700;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 5;
            Projectile.scale = 0.55f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            Player player = Main.player[Projectile.owner];
            bool VisualsOrange = false;
            Color FXColor = Main.rand.NextBool() ? Color.MediumVioletRed : Color.Violet;

            if (time < (float)endTime)
            {
                PRTLoader.NewParticle<PRT_GlowSquareCal>(Projectile.Center, Vector2.Zero, FXColor, 1f).Configure(false, 32, 28, true, Main.rand.NextFloat(-0.2f, 0.2f));
                time = endTime;
                float lerpValue = Utils.GetLerpValue(35f, 0f, time, clamped: true);
                float num = 3f;
                float num2 = 360f / num;
                for (int i = 0; (float)i < num; i++)
                {
                    MathHelper.ToRadians((float)i * num2);
                    Vector2 vector = CEUtils.RandomVelocity(100f, 70f, 250f, 0.04f);
                    vector *= Main.rand.NextFloat(15f, 30f) * lerpValue;
                    PRTLoader.NewParticle<PRT_SparkCal>(Projectile.Center + vector * 2.5f, -vector * Main.rand.NextFloat(0.08f, 0.12f) * 1.5f, FXColor, Main.rand.NextFloat(1.1f, 1.25f) - 0.2f * lerpValue).Configure(false, 14);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + vector * 2.5f, 278, -vector * Main.rand.NextFloat(0.08f, 0.12f) * 1.5f, 0, default(Color), Main.rand.NextFloat(0.4f, 0.6f));
                    dust.noGravity = true;
                    dust.color = FXColor;
                }
            }

            if (time >= (float)endTime)
            {
                Projectile.scale *= 1.15f;
                for (int j = 0; j < 3; j++)
                {
                    PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, FXColor, 0.7f * (float)(j + 1) * Projectile.scale).Configure("CalamityEntropy/Assets/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.7f * (float)(j + 1) * Projectile.scale, 1f * Projectile.scale, 18);
                    PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.White, 0.35f * (float)(j + 1) * Projectile.scale).Configure("CalamityEntropy/Assets/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.35f * (float)(j + 1) * Projectile.scale, 0.5f * Projectile.scale, 18);
                }

                for (int k = 0; k < 6; k++)
                {
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, new Vector2((!VisualsOrange) ? 1 : 0, VisualsOrange ? 1 : 0) * -5f * ((k % 2 != 0) ? 1 : (-1)), FXColor, (0.08f - (float)k * 0.01f) * Projectile.scale).Configure(affectedByGravity: false, 15, new Vector2(5f, 0.8f), quickShrink: true, glow: false, 1.2f);
                }

                if (time == (float)endTime)
                {
                    if (VisualsOrange)
                    {
                        PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, FXColor, 0f).Configure("CalamityEntropy/Assets/Particles/GlowSquareParticleBig", Vector2.One, MathF.PI / 4f, 0f, 2.2f, 22);
                        PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, FXColor, 0f).Configure("CalamityEntropy/Assets/Particles/GlowSquareParticleBig", Vector2.One, MathF.PI / 4f, 0f, 1.2f, 47);
                    }
                    else
                    {
                        PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, FXColor, 0f).Configure("CalamityEntropy/Assets/Particles/HighResHollowCircleHardEdge", Vector2.One, MathF.PI / 4f, 0f, 0.3f, 22);
                    }

                    for (int l = 0; l < 30; l++)
                    {
                        Dust dust2 = Dust.NewDustPerfect(Projectile.Center, VisualsOrange ? 267 : 278, Vector2.One.RotatedByRandom(100.0) * Main.rand.NextFloat(2.5f, 15f));
                        dust2.scale = Main.rand.NextFloat(0.85f, 1.15f) * (VisualsOrange ? 1f : 1.2f);
                        dust2.noGravity = VisualsOrange;
                        dust2.color = Color.Lerp(Color.White, FXColor, 0.5f);
                        if (VisualsOrange)
                        {
                            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.One.RotatedByRandom(100.0) * Main.rand.NextFloat(5.5f, 20f), FXColor, Main.rand.NextFloat(1.3f, 3.8f)).Configure("CalamityEntropy/Assets/Particles/GlowSquareParticle", Vector2.One, MathF.PI / 4f, Main.rand.NextFloat(1.3f, 3.8f), 0.2f, 38);
                        }
                        else
                        {
                            PRTLoader.NewParticle<PRT_CustomSpark>(Projectile.Center, Vector2.One.RotatedByRandom(100.0) * Main.rand.NextFloat(5.5f, 20f), FXColor, Main.rand.NextFloat(2.2f, 4.8f)).Configure("CalamityEntropy/Assets/Particles/Sparkle", false, 38, new Vector2(0.4f, Main.rand.NextFloat(0.9f, 1.4f)), true, true, 0f, false, false);
                        }
                    }
                }
            }

            time += 1f;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
    public class MiracleWreckageHeld : ModProjectile
    {
        public class MWParticle
        {
            public Vector2 offset = Vector2.Zero;
            public Vector2 velocity;
            public Color color;
            public float scale = Main.rand.NextFloat(1f, 1.6f);
            public MWParticle(Vector2 vel)
            {
                velocity = vel;
                int rcl = Main.rand.Next(2);
                if (rcl == 0)
                {
                    color = Main.rand.NextBool() ? Color.Purple : Color.Pink;
                }
                else
                {
                    color = Main.rand.NextBool() ? Color.MediumPurple : Color.Red;
                }

                color *= 0.6f;
            }
            public int counter = 0;
            public float alpha = 1;
            public void update()
            {
                counter++;
                offset += velocity;
                if (counter > 4)
                {
                    alpha *= 0.7f;
                }
                scale *= 0.96f;
            }
        }
        public List<MWParticle> particles = new List<MWParticle>();
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Miracle/MiracleWreckage";
        List<float> odr = new List<float>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.MaxUpdates = 14;
            Projectile.light = 1;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public bool shake = true;
        public int SpeedUp = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int count = 0;
            int type = ModContent.ProjectileType<MiracleWreckageThrow>();
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == type && p.owner == Projectile.owner)
                {
                    if (p.ModProjectile is MiracleWreckageThrow mw && mw.target == target.whoAmI)
                    {
                        count++;
                        mw.PopOut();
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Projectile.velocity.normalize().RotatedByRandom(1) * Main.rand.NextFloat(38, 46), ModContent.ProjectileType<MiracleVortex>(), Projectile.damage, 0, Projectile.owner);
                    }
                }
            }
            if (count >= 6)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<Blackhole>(), Projectile.damage / 8, 0, Projectile.owner, 0, target.whoAmI);
            }
            if (count > 0)
            {
                SpeedUp = 2 + count / 3;
            }
            if (shake)
            {
                CEUtils.PlaySound("DemonSwordInsaneImpact", Main.rand.NextFloat(0.8f, 1.4f), target.Center);
                CEUtils.PlaySound("energyImpact", Main.rand.NextFloat(0.6f, 1.12f), target.Center, 4, 1f * CEUtils.WeapSound);

                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(-(target.Center - Projectile.Center).normalize(), 26));
                shake = false;
            }
            //GlowSquareCal+ShadeDash成对spawn,NonPremultiplied和AlphaBlend混用是旧表现
            //ShadeDash旧NonPremultiplied混合,c1/c2/TL字段Configure前面赋
            for (int i = 0; i < 32; i++)
            {
                Color clr = Main.rand.NextBool() ? new Color(240, 240, 255) : new Color(210, 160, 255);
                var p = PRTLoader.NewParticle<PRT_ShadeDashParticle>(target.Center + CEUtils.randomPointInCircle(26), (target.Center - Projectile.Center).normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(10, 64), Color.White);
                p.c1 = clr;
                p.c2 = clr;
                p.TL = 12;
                p.Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0, 16);
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(220, 220, 255), 3).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 255, 255), 1.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 8.4f;
        }
        public float length = 0;
        public int Dir = -1;
        public int swing = 0;
        public float vsAlpha = 0;
        public float rotVel = 0;
        public bool rcl = true;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Dir);
            writer.Write(swing);
            writer.Write(rotVel);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Dir = reader.ReadInt32();
            swing = reader.ReadInt32();
            rotVel = reader.ReadSingle();
        }
        public bool flag = false;
        public bool flag2 = true;
        public override void AI()
        {
            if (flag2)
            {
                flag2 = false;
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                Dir = Projectile.velocity.X > 0 ? -1 : 1;
            }
            Player owner = Projectile.GetOwner();
            if (!flag)
                if (Projectile.localAI[0]++ > 300)
                    length = float.Lerp(length, (Main.zenithWorld ? 2f : 1.23f), 0.01f);
            if (!owner.dead)
                Projectile.timeLeft = 3;
            owner.Entropy().MouseWorldListener = true;
            Projectile.Center = Projectile.GetOwner().MountedCenter;
            float rot = (owner.Entropy().MouseWorld - Projectile.Center).ToRotation();
            float targetRot = rot + Dir * 2.4f;
            if (Projectile.localAI[1]++ == 0)
                Projectile.rotation = rot + Dir * 1.2f;
            float speed = owner.GetTotalAttackSpeed(DamageClass.Melee);
            if (SpeedUp > 0)
            {
                speed *= 2;
            }
            if (swing < 0)
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.01f, false);
            if (flag)
            {
                length *= 0.997f;
                length -= 0.005f;
                if (length <= 0.04f)
                    Projectile.Kill();
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (!rcl && Main.mouseRight && swing < -16 && ((!flag && length > 0.85f) || (flag && length > 0.32f)))
                {
                    flag = !flag;
                }
                rcl = Main.mouseRight;
            }

            Projectile.rotation += rotVel * speed;
            rotVel *= (float)(Math.Pow(0.987f, speed));
            if (swing < 0)
                Projectile.velocity = rot.ToRotationVector2() * 16;
            swing--;
            if (swing == 0)
                SpeedUp--;
            if (!flag)
            {
                if (length > 0.9f && swing < -16 * Projectile.MaxUpdates / speed / (SpeedUp > 0 ? 2 : 1) && Main.mouseLeft && Main.myPlayer == Projectile.owner)
                {
                    Dir *= -1;
                    shake = true;
                    odr.Clear();
                    Projectile.ResetLocalNPCHitImmunity();
                    swing = (int)(30 * Projectile.MaxUpdates / speed);
                    rotVel = Dir * 0.058f;
                    CEUtils.PlaySound("DemonSwordSwing1", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        CEUtils.SyncProj(Projectile.whoAmI);
                }
            }
            if (Projectile.localAI[1] % 5 == 0 && length > 0.24f)
            {
                for (int i = 0; i < 1; i++)
                {
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + owner.velocity + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(0.2f, float.Max(0.2f, length)) * 522 - (Main.zenithWorld ? Projectile.rotation.ToRotationVector2() * (length * 20) : Vector2.Zero), owner.velocity * 0.4f + Projectile.rotation.ToRotationVector2().RotatedByRandom(0.2f) * Main.rand.NextFloat(32, 42) * length, Color.MediumVioletRed * 0.6f, Main.rand.NextFloat(0.04f, 0.054f) * length).Configure(false, 8, new Vector2(0.34f, 1));
                }
            }
            if (swing > 0)
            {
                if (swing > 25 * Projectile.MaxUpdates)
                { vsAlpha += 0.05f * speed; if (vsAlpha > 1) vsAlpha = 1; }
                if (swing < 20 * Projectile.MaxUpdates)
                    vsAlpha = swing / (20f * Projectile.MaxUpdates);
            }
            else
                vsAlpha = 0;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].update();
                if (particles[i].counter > 60 * (Projectile.ai[1] + 0.5f))
                {
                    particles.RemoveAt(i);
                }

            }
            for (int i = 0; i < 1; i++)
            {
                particles.Add(new MWParticle(new Vector2(Main.rand.NextFloat(15, 17) * length * Projectile.scale, 0).RotatedByRandom(0.025f)) { offset = CEUtils.randomPointInCircle(10) });
                particles[particles.Count - 1].scale *= 2.4f * length * Projectile.scale;
                particles[particles.Count - 1].offset -= (Main.zenithWorld ? new Vector2(length * Projectile.scale * 85, 0) : Vector2.Zero);
            }
            if (Projectile.velocity.X > 0)
            {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            odr.Add(Projectile.rotation);
            if (odr.Count > 90)
            {
                odr.RemoveAt(0);
            }
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D trail = CEExtraAssets.StreakGoop;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            {
                for (int i = 0; i < odr.Count; i++)
                {
                    Color b = new Color(255, 255, 255);
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(220 * Projectile.scale * length, 0).RotatedBy(odr[i])),
                          new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                          b));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(140 * Projectile.scale * length, 0).RotatedBy(odr[i])),
                          new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    var gd = Main.graphics.GraphicsDevice;
                    SpriteBatch sb = Main.spriteBatch;
                    Effect shader = CEEffectAssets.SwordTrail3;

                    sb.End();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                    shader.Parameters["color2"].SetValue((Color.LightBlue).ToVector4());
                    shader.Parameters["color1"].SetValue((Color.Purple).ToVector4());
                    shader.Parameters["uTime"].SetValue(Main.GameUpdateCount * 2);
                    shader.Parameters["alpha"].SetValue(vsAlpha);
                    shader.CurrentTechnique.Passes["EffectPass"].Apply();
                    gd.Textures[0] = CEExtraAssets.Streak2;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }
            {
                ve.Clear();
                for (int i = 0; i < odr.Count; i++)
                {
                    Color b = new Color(255, 255, 255);
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(1000 * (0.5f + Projectile.ai[1]) * Projectile.scale * length, 0).RotatedBy(odr[i])),
                          new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                          b));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(800 * (0.5f + Projectile.ai[1]) * Projectile.scale * length, 0).RotatedBy(odr[i])),
                          new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    var gd = Main.graphics.GraphicsDevice;
                    SpriteBatch sb = Main.spriteBatch;
                    Effect shader = CEEffectAssets.SwordTrail3;

                    sb.End();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                    shader.Parameters["color2"].SetValue((new Color(255, 200, 255)).ToVector4());
                    shader.Parameters["color1"].SetValue((Color.Violet).ToVector4());
                    shader.Parameters["uTime"].SetValue(Main.GameUpdateCount * 2);
                    shader.Parameters["alpha"].SetValue(vsAlpha);
                    shader.CurrentTechnique.Passes["EffectPass"].Apply();
                    gd.Textures[0] = CEExtraAssets.Streak2;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }


            int dir = (int)(Projectile.ai[0]);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor, rot, origin, Projectile.scale * scale * (length < 0.35f && flag ? 0 : 1), effect);

            Texture2D g = CEExtraAssets.Glow;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D c = CEExtraAssets.SemiCircularSmear;
            float alphac = vsAlpha * 0.82f;
            float crot = Projectile.rotation + Dir * -1.2f;
            Main.spriteBatch.Draw(c, Projectile.Center - Main.screenPosition, null, Color.MediumPurple * alphac, crot, c.Size() / 2f, 13f * 0.5f * length * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(c, Projectile.Center - Main.screenPosition, null, Color.Red * alphac * 0.5f, crot, c.Size() / 2f, 5f * (Projectile.ai[1] + 0.5f) * length * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(c, Projectile.Center - Main.screenPosition, null, Color.Pink * alphac * 0.6f, crot, c.Size() / 2f, 10 * (Projectile.ai[1] + 0.5f) * length * Projectile.scale, SpriteEffects.None, 0);
            foreach (var p in particles)
            {
                Main.spriteBatch.Draw(g, Projectile.Center + p.offset.RotatedBy(Projectile.rotation) - Main.screenPosition, null, p.color, Projectile.rotation + p.velocity.ToRotation(), new Vector2(40, 128), new Vector2(1f, 0.25f) * 0.6f * p.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (swing < 0)
                return false;
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.scale * scale * 900 * length * (Projectile.ai[1] + 0.5f), targetHitbox, 420);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.scale * scale * 860 * length * (Projectile.ai[1] + 0.5f), 128, DelegateMethods.CutTiles);
        }
    }
    public class MiracleShoot : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 400;
            Projectile.height = 400;
            Projectile.MaxUpdates = 4;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 90;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.timeLeft < 16)
                return false;
            return null;
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Projectile.timeLeft / 30f;

            Projectile.velocity *= 0.96f;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {

        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D trail = CEExtraAssets.MotionTrail5;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<Vector2> p1 = new List<Vector2>();
            List<Vector2> p2 = new List<Vector2>();
            for (float i = -1; i <= 1; i += 0.01f)
            {
                p2.Add(((i * 1f).ToRotationVector2() * new Vector2(1.2f, 1)).RotatedBy(Projectile.rotation) * 10);
                p1.Add(((i * 1.6f).ToRotationVector2() * new Vector2(1.2f, 1)).RotatedBy(Projectile.rotation) * 320);
            }
            for (int i = 0; i < p1.Count; i++)
            {
                Color b = new Color(230, 220, 255);
                ve.Add(new ColoredVertex(Projectile.Center + Projectile.rotation.ToRotationVector2() * -180 - Main.screenPosition + p1[i],
                      new Vector3((i) / ((float)p1.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center + Projectile.rotation.ToRotationVector2() * -180 - Main.screenPosition + p2[i],
                      new Vector3((i) / ((float)p1.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = CEEffectAssets.SwordTrail4;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue(Color.AliceBlue.ToVector4());
                shader.Parameters["color1"].SetValue((Color.MediumPurple).ToVector4());
                shader.Parameters["alpha"].SetValue(Projectile.Opacity);
                shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 200);
                gd.Textures[1] = CEExtraAssets.PatchyTallNoise;
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;

                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

            }
            return false;
        }
    }

}
