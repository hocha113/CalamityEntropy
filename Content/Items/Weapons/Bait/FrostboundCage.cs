using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public class FrostboundCage : ModItem, IBaitItem
    {
        public static int TagDamage = 5;
        public static float DamageMult = 1f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 35;
            Item.knockBack = 0;
            Item.shootSpeed = 36;
            Item.useAnimation = Item.useTime = 30;
            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ItemRarityID.Pink;
            Item.width = 68;
            Item.height = 68;
            Item.autoReuse = false;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<FrostboundCageProjectile>();
            Item.autoReuse = true;
        }
        public override bool CanUseItem(Player player)
        {
            return player.Entropy().BaitUsable;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, (int)(damage * DamageMult), knockback, player.whoAmI, 0, 0, TagDamage);
            return false;
        }

        public override void AddRecipes()
        {
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class FrostboundCageProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<FrostboundCage>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, true, -1);
            Projectile.width = Projectile.height = 12;
        }

        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.GetOwner().Entropy().BaitCharge--;
            }
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (StickNPC < 0)
            {
                if (Counter > 16)
                {
                    Projectile.velocity *= 0.99f;
                    Projectile.velocity.Y += 0.8f;
                }
            }
            else
            {
                NPC npc = StickNPC.ToNPC();
                if (!npc.active)
                {
                    Projectile.Kill();
                    return;
                }
                Main.player[Projectile.owner].MinionAttackTargetNPC = npc.whoAmI;
                npc.GetGlobalNPC<WhipDebuffNPC>().BaitStick = 2;
                if (IsActive)
                {
                    Projectile.GetOwner().Calamity().mouseWorldListener = true;
                    npc.GetGlobalNPC<WhipDebuffNPC>().ClearBaitTags();
                    npc.GetGlobalNPC<WhipDebuffNPC>().Tags.Add(new WhipTag(this.GetType().Name, 5, this.TagDamage, 1, 0, this.GetType().Name) { IsABaitTag = true });
                }
                Projectile.Center = npc.Center + StickOffset;
                ActiveCounter++;
                if(ActiveCounter > 120)
                {
                    if(IsActive)
                    {
                        SetActive();
                        CEUtils.SyncProj(Projectile.whoAmI);
                    }
                }
                else
                {
                    if(ActiveCounter % 20 == 0 && !Main.dedServ)
                    {
                        shake2 = 1;
                        CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), 1, Projectile.Center);
                    }
                }
            }
            activeEffectAlpha = float.Lerp(activeEffectAlpha, (StickNPC >= 0 && IsActive) ? 1 : 0, 0.04f);
            Counter++;
            shake2 *= 0.85f;
        }
        public override void ActiveEffect(float damageMul)
        {
            if (IsActive)
            {
                IsActive = false;
                if (!Main.dedServ)
                {
                    ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.NoDirQuickShake(32), Main.LocalPlayer.Distance(Projectile.Center), 1800);
                    for (int i = 0; i < 32; i++)
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.1f, 1) * 26, false, 18, 0.06f * Main.rand.NextFloat(0.3f, 1f), Color.LightSkyBlue, new Vector2(0.32f, 1f)));
                    if (Main.myPlayer == Projectile.owner)
                    {
                        CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, Projectile.damage * 5, 180, Projectile.DamageType);
                        for (int i = 0; i < 5; i++)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(24, 32), ModContent.ProjectileType<FrostboundSpirit>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner, 0, Main.rand.Next(0, 30));
                        }
                    }
                    CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), 1, Projectile.Center);
                    CEUtils.PlaySound("soulScreem", 0.6f, Projectile.Center);
                    CEUtils.PlaySound("explosion1", 1, Projectile.Center);
                }
                Projectile.Kill();
            }
        }
        public float activeEffectAlpha = 0;
        public float shake2 = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            float shake = ActiveCounter / 120f;
            Vector2 offset = CEUtils.randomPointInCircle(shake * 20 * shake2);
            Main.spriteBatch.UseAdditiveClamp();
            if (activeEffectAlpha >= 0.01f)
            {
                Texture2D pulse = CEUtils.getExtraTex("ShatteredExplosion");
                for(float i = 0; i < 1f; i += 0.1f)
                {
                    float scale = CEUtils.Frac(i + Main.GlobalTimeWrappedHourly * 4f);
                    Main.spriteBatch.Draw(pulse, Projectile.Center + offset  - Main.screenPosition, null, Color.LightSkyBlue * 0.86f * Projectile.Opacity * (1 - scale * scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.2f * shake, SpriteEffects.None, 0);
                }
            }
            float s2 = 1 - shake2;
            s2 = 1 - s2 * s2 * s2;
            
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
            {
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White * s2, null, Projectile.Center + i.ToRotationVector2() * 7 + offset));
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos:Projectile.Center +  offset));
            return false;
        }
        public override bool? CanDamage()
        {
            return StickNPC == -1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.tileCollide = false;
            OnHitEffect(Projectile.Center);
            Projectile.velocity *= 0;
            StickNPC = target.whoAmI;
            StickOffset = Projectile.Center - target.Center;
            Projectile.timeLeft = 480;
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), 1, Projectile.Center);
            for (int i = 0; i < 32; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.3f, 1) * 24, false, 16, 0.08f * Main.rand.NextFloat(0.6f, 1f), Color.LightSkyBlue, new Vector2(0.2f, 1f)));
            for (int i = 0; i < 4; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, (i * MathHelper.PiOver2).ToRotationVector2() * 30, false, 20, 0.12f, Color.LightSkyBlue, new Vector2(0.16f, 1f)));
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(Projectile.Center, 18 * Projectile.scale).Intersects(targetHitbox);
        }
        public override void OnKill(int timeLeft)
        {
            if (IsActive)
            {
                SetActive();
                IsActive = false;
                CEUtils.SyncProj(Projectile.whoAmI);
            }
            float scale = 4;
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.LightSkyBlue * 1.14f, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.LightSkyBlue * 1.12f, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.032f, 18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.LightSkyBlue * 1.12f, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.024f, 15));
        }
    }
    public class FrostboundSpirit : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 64;
            Projectile.timeLeft = 300;
            Projectile.light = 1;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> OldRots = new List<float>();
        public override bool? CanDamage()
        {
            return false;
        }
        public float Counter
        {
            get { return Projectile.ai[0]; }
            set { Projectile.ai[0] = value; }
        }
        public int ShootDelay = Main.rand.Next(70, 100);
        public override void AI()
        {
            if (Projectile.localAI[1] == 0)
                Projectile.localAI[1] = Main.rand.NextFloat(MathHelper.Pi, MathHelper.Pi * 3);
            Projectile.Opacity = 1;
            if (Projectile.timeLeft < 20)
                Projectile.Opacity = Projectile.timeLeft / 20f;
            if (Projectile.timeLeft > 280)
                Projectile.Opacity = (300 - Projectile.timeLeft) / 20f;
            Player player = Projectile.GetOwner();
            Projectile.frameCounter++;
            if(Projectile.frameCounter % 3 == 0)
            {
                Projectile.frame++;
                if(Projectile.frame > 3)
                {
                    Projectile.frame = 0;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft <= 120)
            {
                if (Projectile.velocity.Length() < 32)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY) * (Projectile.velocity.Length() + 2f);
            }
            else
            {
                ShootDelay--;
                NPC target = Projectile.FindMinionTarget();
                if (target != null)
                {
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 0.036f, false);
                    Projectile.velocity = Projectile.rotation.ToRotationVector2() * Projectile.velocity.Length();
                    if(ShootDelay <= 0)
                    {
                        ShootDelay = Main.rand.Next(26, 38);
                        if(Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity * 1.25f, ModContent.ProjectileType<FrostShoot>(), Projectile.damage, 6, Projectile.owner);
                        }
                    }
                }
                else
                {
                    if (Projectile.timeLeft > 120)
                        Projectile.timeLeft--;
                }
            }
            Counter++;
            var adv = Projectile.velocity.RotatedBy((float)(Math.Sin(Counter * 0.2f + Projectile.localAI[1])) * 0.6f);
            Projectile.rotation = adv.ToRotation();
            for (float i = 0; i < 1f; i += 0.1f)
            {
                oldPos.Add(Projectile.Center + adv * i);
                OldRots.Add(Projectile.rotation);
                if (oldPos.Count > 120)
                {
                    oldPos.RemoveAt(0);
                    OldRots.RemoveAt(0);
                }
            }
            Projectile.position += adv;

        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseAdditiveClamp();
            for(int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, CEUtils.GetCutTexRect(tex, 4, Projectile.frame, false), Color.White * Projectile.Opacity * 0.3f * p, OldRots[i], tex.Size() * new Vector2(0.5f, 0.5f / 4f), Projectile.scale, SpriteEffects.None, 0);
            }
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver2)
            {
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + (i + Main.GlobalTimeWrappedHourly * 8).ToRotationVector2() * 4, CEUtils.GetCutTexRect(tex, 4, Projectile.frame, false), Color.White * 0.9f * Projectile.Opacity, Projectile.rotation, tex.Size() * new Vector2(0.5f, 0.5f / 4f), Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tex, 4, Projectile.frame, false), Color.White * Projectile.Opacity, Projectile.rotation, tex.Size() * new Vector2(0.5f, 0.5f / 4f), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
    public class FrostShoot : EBookBaseProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, 1);
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.timeLeft = 360;
            Projectile.MaxUpdates = 1;
            Projectile.light = 1;
        }
        public List<Vector2> odp = new List<Vector2>();
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("LuminarArrowHit", Main.rand.NextFloat(0.7f, 1.2f), Projectile.Center);
            }
            if (Projectile.localAI[0]++ > 10)
                Projectile.HomingToNPCNearby(3.8f, 0.952f, 2000);
            for (float i = 0.05f; i <= 1f; i += 0.05f)
            {
                odp.Add(Projectile.Center + Projectile.velocity * i);
                if (odp.Count > 190)
                {
                    odp.RemoveAt(0);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.AddLight(Projectile.Center, Color.Silver, Projectile.scale);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.GetGlobalNPC<WhipDebuffNPC>().BaitStick > 0)
            {
                target.GetGlobalNPC<WhipDebuffNPC>().BaitStick = 0;
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.ModProjectile != null && p.ModProjectile is BaitProj ibp && ibp.StickNPC == target.whoAmI)
                    {
                        if (!ibp.IsActive)
                            p.Kill();
                    }
                }
            }
            OnKill(Projectile.timeLeft);
        }
        public Color EffectColor()
        {
            return new Color(120, 190, 255);
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            if (timeLeft > 0)
            {
                float scale = Projectile.scale;
                CEUtils.PlaySound("ThalassianHit", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
                for (int i = 0; i < 4; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.7f, 1f) * scale * 3.2f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(0.4f, 1f) * 40 * scale;
                    dust.noGravity = false;
                    dust.color = EffectColor();
                    dust.fadeIn = 2f;
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            for (int i = 0; i < odp.Count; i++)
            {
                float p = (i / (odp.Count - 1f));
                float alpha = p < 0.7f ? p / 0.7f : 1;
                float width = p;
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 11 * Projectile.scale * width, 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(100, 190, 255), EffectColor());
            Main.spriteBatch.UseAdditiveClamp();
            Texture2D ar = CEUtils.getExtraTex("SpearArrowGlow2");
            Main.spriteBatch.Draw(ar, Projectile.Center - Main.screenPosition, null, new Color(80, 90, 255), Projectile.rotation, ar.Size() * 0.5f, Projectile.scale * 0.3f, SpriteEffects.None, 0); Main.spriteBatch.Draw(ar, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, ar.Size() * 0.5f, Projectile.scale * 0.22f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
