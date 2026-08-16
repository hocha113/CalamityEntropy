using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class BuriedSun : ModItem
    {
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Color c = Color.Lerp(Color.LightGreen * 1.4f, new Color(20, 20, 20), (float)((Math.Sin(Main.GlobalTimeWrappedHourly * 8) + 1.0) / 2.0));
            tooltips.Replace("f0ffe6", c.Hex3());
            tooltips.Replace("f0ffe6", c.Hex3());
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.width = 134;
            Item.height = 38;
            Item.damage = 120;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 9;
            Item.useAnimation = 9;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.value = CalamityGlobalItem.RarityRedBuyPrice;
            Item.rare = ItemRarityID.Red;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 8f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 14;
            Item.noUseGraphic = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<BuriedSunHoldout>(), damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<MeldBlob>(), 18)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class BuriedSunHoldout : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/BuriedSun";
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 2;

        }

        public int MaxCharge => Projectile.GetOwner().HeldItem.useTime;
        public int Charge = 0;
        public int Delay = 30;
        public int ShootCount = 3;
        public Player player => Projectile.GetOwner();
        public Vector2 FirePos => Projectile.Center + Projectile.velocity.normalize() * 112 * Projectile.scale;
        public void Shoot()
        {
            ShootCount--;
            Lighting.AddLight(base.Projectile.Center, Color.LightGreen.ToVector3() * 0.36f);
            for (int i = 0; i <= 10; i++)
            {
                float num2 = Main.rand.NextFloat(-0.7f, 0.7f);
                int type2 = ModContent.DustType<VoidDustInverted>();
                Dust dust2 = Dust.NewDustPerfect(FirePos, type2);
                dust2.scale = Main.rand.NextFloat(1.7f, 1.9f) - Math.Abs(num2);
                dust2.velocity = (base.Projectile.velocity * 2f).RotatedBy(num2) * Main.rand.NextFloat(0.35f, 1f) * (1f - Math.Abs(num2));
                dust2.noGravity = true;
                dust2.color = Color.LightGreen;
            }
            CEUtils.PlaySound("CursedDaggerThrow", Main.rand.NextFloat(2f, 2.4f), FirePos, 16, 0.3f);
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), FirePos, Projectile.velocity.normalize() * player.HeldItem.shootSpeed, ModContent.ProjectileType<BuriedShoot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }

        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Delay = Projectile.GetOwner().HeldItem.useTime * 2;
            }
            Projectile.StickToPlayer();
            player.SetHandRot(Projectile.rotation);
            player.itemTime = player.itemAnimation = 3;
            if (ShootCount > 0)
            {
                Charge++;
                if (Charge > MaxCharge)
                {
                    Charge = 0;
                    Shoot();
                }
            }
            else
            {
                Delay--;
                if (Delay <= 0)
                {
                    Projectile.Kill();
                    player.itemTime = player.itemAnimation = 1;
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.EntitySpriteDraw(Projectile.GetTexture(), Projectile.Center + Projectile.velocity.normalize() * 32 + player.gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor, Projectile.rotation, Projectile.GetTexture().Size().Half(), Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            float gScale = Charge / ((float)MaxCharge);
            CEUtils.DrawGlow(FirePos, Color.Black, gScale * 0.6f, false);
            CEUtils.DrawGlow(FirePos, Color.Black, gScale * 0.6f, false);
            CEUtils.DrawGlow(FirePos, Color.White, gScale * 0.32f, true);
            CEUtils.DrawGlow(FirePos, Color.White, gScale * 0.32f, true);
            return false;
        }
    }
    public class BuriedShoot : ModProjectile
    {
        public Color InnerColor = Color.LightGreen;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[base.Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 13;
            Projectile.timeLeft = 12 * Projectile.MaxUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public int OrigDamage = 0;
        public void SpawnParticle()
        {
            SpawnParticle(Projectile.Center);
        }
        public void SpawnParticle(Vector2 pos)
        {
            GeneralParticleHandler.SpawnParticle(new CustomSpark(pos, Vector2.Zero, "CalamityMod/Particles/LargeBloom", affectedByGravity: false, 8, 0.24f, Color.Black, Vector2.One, useAddativeBlend: false));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(pos, Vector2.Zero, "CalamityMod/Particles/LargeBloom", affectedByGravity: false, 8, 0.12f, Color.White, Vector2.One), pixelate: false, GeneralDrawLayer.AfterEverything);
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                OrigDamage = Projectile.damage;
                SpawnParticle();
            }
            if (base.Projectile.timeLeft % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + Projectile.velocity.normalize() * 40, -base.Projectile.velocity * 0.05f, "CalamityMod/Particles/GlowSpark2", affectedByGravity: false, 9, 0.052f, Color.Black, new Vector2(0.6f, 1.3f), useAddativeBlend: false));
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + Projectile.velocity.normalize() * 40, -base.Projectile.velocity * 0.05f, "CalamityMod/Particles/GlowSpark", affectedByGravity: false, 9, 0.022f, Color.LightGreen, new Vector2(0.6f, 1.9f)), pixelate: false, GeneralDrawLayer.AfterEverything);
            }
        }
        public override void OnKill(int timeLeft)
        {
            Vector2 spos = Projectile.Center + Projectile.velocity.normalize() * 68;
            SpawnParticle(spos);
            if(Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), spos, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(16, 24), ModContent.ProjectileType<BuriedDot>(), OrigDamage, Projectile.knockBack, Projectile.owner);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnParticle();
            if (Projectile.timeLeft > 60)
                Projectile.timeLeft = 60;
            SoundStyle style = DeadSunsWind.Explosion with
            {
                Pitch = 0.6f + Main.rand.NextFloat(-0.2f, 0.2f),
                Volume = 0.2f
            };
            SoundEngine.PlaySound(in style, base.Projectile.Center);
            for (int i = 0; i <= 16; i++)
            {
                float num2 = Main.rand.NextFloat(-0.7f, 0.7f);
                int type2 = ModContent.DustType<VoidDustInverted>();
                Dust dust2 = Dust.NewDustPerfect(Projectile.Center, type2);
                dust2.scale = Main.rand.NextFloat(1.2f, 1.4f) - Math.Abs(num2);
                dust2.velocity = (base.Projectile.velocity * 2f).RotatedBy(num2) * Main.rand.NextFloat(0.35f, 1f) * (1f - Math.Abs(num2)) * 1.4f;
                dust2.noGravity = true;
                dust2.color = Color.LightGreen;

                Vector2 v = (base.Projectile.velocity * 2f).RotatedBy(num2 * 0.6f) * Main.rand.NextFloat(0.4f, 1f) * (1f - Math.Abs(num2)) * 3.6f;

                float sc = Main.rand.NextFloat(0.6f, 1);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + v, v, "CalamityMod/Particles/GlowSpark2", affectedByGravity: false, 16, 0.06f * sc, Color.Black, new Vector2(0.6f, 1.3f), useAddativeBlend: false));
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + v, v, "CalamityMod/Particles/GlowSpark", affectedByGravity: false, 16, 0.022f * sc, Color.LightGreen, new Vector2(0.6f, 1.9f)), pixelate: false, GeneralDrawLayer.AfterEverything);
            }
        }
    }
    public class BuriedShoot2 : ModProjectile
    {
        public Color InnerColor = Color.LightGreen;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[base.Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 13;
            Projectile.timeLeft = 12 * Projectile.MaxUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public void SpawnParticle()
        {
            SpawnParticle(Projectile.Center);
        }
        public void SpawnParticle(Vector2 pos)
        {
            GeneralParticleHandler.SpawnParticle(new CustomSpark(pos, Vector2.Zero, "CalamityMod/Particles/LargeBloom", affectedByGravity: false, 8, 0.24f, Color.Black, Vector2.One, useAddativeBlend: false));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(pos, Vector2.Zero, "CalamityMod/Particles/LargeBloom", affectedByGravity: false, 8, 0.12f, Color.White, Vector2.One), pixelate: false, GeneralDrawLayer.AfterEverything);
        }
        public override void AI()
        {
            if (base.Projectile.timeLeft % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + Projectile.velocity.normalize() * 40, -base.Projectile.velocity * 0.05f, "CalamityMod/Particles/GlowSpark2", affectedByGravity: false, 9, 0.052f, Color.Black, new Vector2(0.6f, 1.3f), useAddativeBlend: false));
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + Projectile.velocity.normalize() * 40, -base.Projectile.velocity * 0.05f, "CalamityMod/Particles/GlowSpark", affectedByGravity: false, 9, 0.022f, Color.LightGreen, new Vector2(0.6f, 1.9f)), pixelate: false, GeneralDrawLayer.AfterEverything);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnParticle();
            if (Projectile.timeLeft > 50)
                Projectile.timeLeft = 50;
            SoundStyle style = DeadSunsWind.Explosion with
            {
                Pitch = 1.2f + Main.rand.NextFloat(-0.2f, 0.2f),
                Volume = 0.16f
            };
            SoundEngine.PlaySound(in style, base.Projectile.Center);
            for (int i = 0; i <= 16; i++)
            {
                float num2 = Main.rand.NextFloat(-0.7f, 0.7f);
                int type2 = ModContent.DustType<VoidDustInverted>();
                Dust dust2 = Dust.NewDustPerfect(Projectile.Center, type2);
                dust2.scale = Main.rand.NextFloat(1.2f, 1.4f) - Math.Abs(num2);
                dust2.velocity = (base.Projectile.velocity * 2f).RotatedBy(num2) * Main.rand.NextFloat(0.35f, 1f) * (1f - Math.Abs(num2)) * 1.4f;
                dust2.noGravity = true;
                dust2.color = Color.LightGreen;

                Vector2 v = (base.Projectile.velocity * 2f).RotatedBy(num2 * 0.6f) * Main.rand.NextFloat(0.4f, 1f) * (1f - Math.Abs(num2)) * 3.6f;

                float sc = Main.rand.NextFloat(0.6f, 1);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + v, v, "CalamityMod/Particles/GlowSpark2", affectedByGravity: false, 16, 0.06f * sc, Color.Black, new Vector2(0.6f, 1.3f), useAddativeBlend: false));
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center + v, v, "CalamityMod/Particles/GlowSpark", affectedByGravity: false, 16, 0.022f * sc, Color.LightGreen, new Vector2(0.6f, 1.9f)), pixelate: false, GeneralDrawLayer.AfterEverything);
            }
        }
    }
    public class BuriedDot : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override void AI()
        {
            Projectile.velocity *= 0.92f;
            if (Projectile.timeLeft < 60)
            {
                Projectile.Opacity = 60f / Projectile.timeLeft;
            }
            else
            {
                if (Projectile.ai[0] == 0)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        List<Projectile> projs = new();
                        foreach (Projectile p in Main.ActiveProjectiles)
                        {
                            if (p.whoAmI == Projectile.whoAmI)
                                continue;
                            if (p.owner == Projectile.owner && p.type == Projectile.type && p.timeLeft > 60 && p.ai[0] == 0 && p.Distance(Projectile.Center) < 800)
                            {
                                projs.Add(p);
                            }
                        }
                        projs = projs.Select(proj => new
                        {
                            Proj = proj,
                            Dist = Vector2.Distance(Projectile.Center, proj.Center)
                        }).OrderBy(p => p.Dist)
                        .Take(2)
                        .Select(x => x.Proj)
                        .ToList();
                        if (projs.Count == 2)
                        {
                            Projectile.ai[0] = projs[0].ai[0] = projs[1].ai[0] = 1;

                            Projectile.ai[1] = projs[0].whoAmI;
                            Projectile.ai[2] = projs[1].whoAmI;

                            projs[0].ai[1] = Projectile.whoAmI;
                            projs[0].ai[2] = projs[1].whoAmI;

                            projs[1].ai[1] = Projectile.whoAmI;
                            projs[1].ai[2] = projs[0].whoAmI;

                            CEUtils.SyncProj(Projectile.whoAmI);
                            CEUtils.SyncProj(projs[0].whoAmI);
                            CEUtils.SyncProj(projs[1].whoAmI);
                        }
                    }
                }
                if (Projectile.ai[0] > 0)
                {
                    if (!((int)Projectile.ai[1]).ToProj().active || !((int)Projectile.ai[2]).ToProj().active)
                    {
                        Projectile.ai[0] = 0;
                        Projectile.ai[1] = Projectile.ai[2] = 0;
                    }
                }
                if (Projectile.ai[0] > 0)
                {
                    var p1 = ((int)Projectile.ai[1]).ToProj();
                    var p2 = ((int)Projectile.ai[2]).ToProj();
                    Vector2 mid = (Projectile.Center / 3f + p1.Center / 3f + p2.Center / 3f);
                    Projectile.timeLeft = 400;
                    Projectile.ai[0]++;
                    if (Projectile.ai[0] == 8)
                    {
                        if (Main.myPlayer == Projectile.owner)
                        {
                            NPC target = CEUtils.FindTarget_HomingProj(Projectile, mid, 1200);
                            if (target != null)
                            {
                                Item gun = new Item(ModContent.ItemType<BuriedSun>());
                                    Vector2 start = Projectile.Center;
                                if (Projectile.GetOwner().PickAmmo(gun, out int projType, out float speed, out int _, out float kb, out int _, false))
                                {
                                    int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), start, (target.Center - start).normalize() * speed * 1.6f, projType, Projectile.damage / 3, kb, Projectile.owner);
                                    p.ToProj().Entropy().buriedShoot = true;
                                    CEUtils.SyncProj(p);
                                }
                            }
                        }
                    }
                    if (Projectile.ai[0] > 26)
                    {
                        SpawnParticle(Projectile.Center, 1.2f);
                        SpawnParticle(p1.Center, 1.2f);
                        SpawnParticle(p2.Center, 1.2f);
                        SpawnParticle(mid, 1.4f);
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 start = i == 0 ? Projectile.Center : (i == 1 ? p1.Center : p2.Center);
                            for (float l = 0.04f; l <= 0.96f; l += 0.02f)
                            {
                                Vector2 p = Vector2.Lerp(start, mid, l);
                                float scale = CEUtils.Parabola(l, 0.8f) + 0.2f;
                                scale *= 0.6f;
                                Vector2 v = (mid - start).normalize();

                                GeneralParticleHandler.SpawnParticle(new CustomSpark(p, v, "CalamityMod/Particles/GlowSpark2", affectedByGravity: false, 8, 0.06f * scale, Color.Black, new Vector2(0.6f, 1.3f), useAddativeBlend: false));
                                GeneralParticleHandler.SpawnParticle(new CustomSpark(p, v, "CalamityMod/Particles/GlowSpark", affectedByGravity: false, 8, 0.022f * scale, Color.LightGreen, new Vector2(0.6f, 1.9f)), pixelate: false, GeneralDrawLayer.AfterEverything);
                            }
                        }
                        NPC target = CEUtils.FindTarget_HomingProj(Projectile, mid, 1200);
                        if (target != null)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), mid, (target.Center - mid).normalize() * 10, ModContent.ProjectileType<BuriedShoot2>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                        ExpParticle(mid);

                        Projectile.Kill();
                        p1.Kill();
                        p2.Kill();
                    }
                }
            }
            SpawnParticle(Projectile.Center, 0.54f * Projectile.Opacity);
            int type = ModContent.DustType<VoidDustInverted>();
            Dust dust2 = Dust.NewDustPerfect(Projectile.Center, type);
            dust2.velocity = CEUtils.randomPointInCircle(4);
            dust2.scale = Projectile.Opacity * 1f;

        }
        public void ExpParticle(Vector2 pos)
        {
            Color color1 = Color.LightGreen;
            Color color2 = Color.Black;
            float ExplosionRadius = 60;
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(pos, Vector2.Zero, color1, Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, ExplosionRadius * 0.0065f + 0.1f, Main.rand.Next(15, 22)), pixelate: false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(pos, Vector2.Zero, Color.Black, Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, ExplosionRadius * 0.0045f + 0.1f, Main.rand.Next(15, 22), UseAdditiveBlend: false));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(pos, Vector2.Zero, Color.Black, Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, ExplosionRadius * 0.003f + 0.1f, Main.rand.Next(15, 22), UseAdditiveBlend: false));
            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, color1, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, ExplosionRadius * 0.005f + 0.05f, 25), pixelate: false, GeneralDrawLayer.AfterEverything);
            }

            float num = ExplosionRadius * 0.1f + 10f;
            float num2 = 360f / num;
            for (int j = 0; (float)j < num; j++)
            {
                float num3 = MathHelper.ToRadians((float)j * num2);
                Vector2 vector = (Vector2.UnitX * Main.rand.NextFloat(ExplosionRadius * 0.2f, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Vector2 vector2 = (Vector2.UnitX * Main.rand.NextFloat(ExplosionRadius * 0.2f, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Dust dust = Dust.NewDustPerfect(pos + vector, Main.rand.NextBool(4) ? ModContent.DustType<LightDust>() : (Main.rand.NextBool() ? 278 : ModContent.DustType<VoidDustInverted>()), vector2);
                dust.noGravity = dust.type != 278;
                dust.color = color1;
                dust.velocity = vector2;
                dust.scale = ((dust.type == 278) ? Main.rand.NextFloat(0.7f, 1.3f) : Main.rand.NextFloat(1.6f, 2.2f));
            }
        }
        public void SpawnParticle(Vector2 pos, float scale)
        {
            GeneralParticleHandler.SpawnParticle(new CustomSpark(pos, Vector2.Zero, "CalamityMod/Particles/LargeBloom", affectedByGravity: false, 8, 0.24f * scale, Color.Black, Vector2.One, useAddativeBlend: false));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(pos, Vector2.Zero, "CalamityMod/Particles/LargeBloom", affectedByGravity: false, 8, 0.12f * scale, Color.White, Vector2.One), pixelate: false, GeneralDrawLayer.AfterEverything);
        }
    }
}
