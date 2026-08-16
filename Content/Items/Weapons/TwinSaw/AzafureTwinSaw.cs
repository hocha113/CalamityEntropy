using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Weapons.Swirlblades;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.TwinSaw
{
    public class AzafureTwinSaw : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 10;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.autoReuse = true;
            Item.DamageType = ModContent.GetInstance<TrueMeleeDamageClass>();
            Item.damage = 7;
            Item.knockBack = 6;
            Item.crit = 5;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<AzafureTwinSawHeld>();
            Item.shootSpeed = 12;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public int UseCount = 0;
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(ItemID.IronBar, 8)
                .AddIngredient(ItemID.Wood, 6)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(ItemID.LeadBar, 8)
                .AddIngredient(ItemID.Wood, 6)
                .AddTile(TileID.Anvils)
                .Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (UseCount == 2)
            {
                Projectile.NewProjectile(source, position, velocity, type, damage, 0, player.whoAmI, 1, 1); 
                Projectile.NewProjectile(source, position, velocity, type, damage, 0, player.whoAmI, -1, 1);
            }
            else
            {
                int dir = UseCount == 0 ? 1 : -1;
                Projectile.NewProjectile(source, position, velocity, type, (int)(damage * 1.5f), 0, player.whoAmI, dir, 0);
            }
            UseCount++;
            if(UseCount > 2)
                UseCount = 0;
            return false;
        }
    }

    public class AzafureTwinSawHeld : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(ModContent.GetInstance<TrueMeleeDamageClass>(), false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }
        public bool Hitted = false;
        public int Target = -1;
        public float Counter { get { return Projectile.localAI[1]; } set { Projectile.localAI[1] = value; } }
        public float Rotation = 0;
        public float sAlpha = 0;
        public float rVel = 0;
        public override bool? CanDamage()
        {
            return (Projectile.ai[1] == 0 && Counter <= 36) || (Projectile.ai[1] > 0 && Counter <= 60);
        }
        public override void AI()
        {
            var player = Projectile.GetOwner();
            player.Calamity().mouseWorldListener = true;
            if (Projectile.localAI[0]++ == 0)
            {
                CEUtils.PlaySound("HellkiteSwing2", Main.rand.NextFloat(1.4f, 1.7f), Projectile.Center, 8, CEUtils.WeapSound * 0.4f);
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_ * 1.5f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            Projectile.Center = player.MountedCenter;
            Projectile.velocity = Projectile.velocity.Length() * (player.Calamity().mouseWorld - Projectile.Center).normalize();
            if (Projectile.ai[0] > 0)
            {
                player.heldProj = Projectile.whoAmI;
            }
            
            int MaxTime = (int)(16 / player.GetTotalAttackSpeed(Projectile.DamageType));
            float p = Projectile.localAI[0] / MaxTime;
            bool CollideTarget()
            {
                NPC npc = Target >= 0 ? Target.ToNPC() : null;
                if (npc == null)
                    return false;
                return Projectile.Colliding(Projectile.getRect(), npc.Hitbox);
            }
            float cr = 5;
            if (Target >= 0 && !Target.ToNPC().active)
                Target = -1;
            if (Projectile.ai[1] == 0)
            {
                if(Hitted)
                {
                    if (Counter < 36)
                    {
                        if (CollideTarget())
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Rotation -= 0.025f;
                                Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                                if (!CollideTarget())
                                    break;
                            }
                            Rotation += 0.175f;
                        }
                        else
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Rotation += 0.025f;
                                Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                                if (CollideTarget())
                                    break;
                            }
                            Rotation += 0.15f;
                        }
                    }
                    if(Counter++ == 36)
                    {
                        rVel = -0.6f;
                    }
                    if (Counter > 38)
                        Projectile.Opacity -= 1 / 7f;
                    if (Counter > 46)
                    {
                        Projectile.Kill();
                        return;
                    }
                    Rotation += rVel;
                    rVel *= 0.74f;
                    Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                    if (Rotation > cr / 2)
                        Projectile.Kill();
                }
                else
                {
                    Rotation = (CEUtils.Parabola(p * 0.5f, 1) - 0.5f) * cr;
                    Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                    sAlpha = 1 - CEUtils.Parabola(p, 1);
                    sAlpha = 1 - (sAlpha * sAlpha);
                    if (p >= 1)
                        Projectile.Kill();
                }
            }
            else
            {
                sAlpha = 0;
                if (Projectile.localAI[0] == 6 && !Main.dedServ)
                {
                    CEUtils.PlaySound("SawShot1", 1.4f, Projectile.Center);
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 pos = sawOrigin + (Projectile.rotation + MathHelper.PiOver2 * dir).ToRotationVector2() * 14 * Projectile.scale;
                        Vector2 vel = Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * dir).RotatedByRandom(1.4f) * Main.rand.NextFloat(28, 32);
                        Color color = Main.rand.NextBool() ? Color.Orange : Color.Firebrick;
                        float scale = Main.rand.NextFloat(1.8f, 2.4f);
                        GeneralParticleHandler.SpawnParticle(new LineParticle(pos, vel, true, Main.rand.Next(8, 12), scale, color));
                    }
                }
                if (Projectile.localAI[0] >= 6)
                {
                    int t = 60;
                    Counter++;
                    if (Counter < t)
                    {
                        Rotation = -0.07f;
                        if(Main.myPlayer == Projectile.owner && Projectile.ai[0] > 0 && Counter > 1)
                        {
                            Vector2 pos = sawOrigin + (Projectile.rotation + MathHelper.PiOver2 * dir).ToRotationVector2() * 14 * Projectile.scale;
                            Vector2 vel = Projectile.rotation.ToRotationVector2() * (player.AzafureEnhance() ? 10 : 7) * Projectile.scale;
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<AzafureSawSpark>(), Projectile.damage / 5 + 1, 0, Projectile.owner);
                        }
                        if(Projectile.ai[0] > 0 && Counter == 4)
                        {
                            CEUtils.PlaySound("chainsaw", Main.rand.NextFloat(1.4f, 1.5f), Projectile.Center, 32, 0.6f * CEUtils.WeapSound);
                        }
                    }
                    if (Counter == t)
                    {
                        rVel = -0.52f;
                    }
                    if (Counter > t + (Projectile.ai[0] > 0 ? -1 : 0))
                        Projectile.Opacity -= 1 / 7f;
                    if (Counter > t + 8)
                    {
                        foreach (Projectile pj in Main.ActiveProjectiles)
                        {
                            if (pj.type == Projectile.type && pj.owner == Projectile.owner)
                            {
                                pj.Kill();
                                pj.scale = 0;
                            }
                        }
                        return;
                    }
                    Rotation += rVel;
                    rVel *= 0.74f;
                    Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                }
                else
                {
                    Rotation = (CEUtils.Parabola(p * 0.5f, 1) - 0.5f) * cr;
                    Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                }
            }
            player.itemTime = player.itemAnimation = 3;
            float r = Projectile.rotation;
            if (Projectile.ai[0] > 0)
            {
                if (r.ToRotationVector2().X > 0)
                {
                    player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
                }
                else
                {
                    player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
                }
            }
            else
            {
                if (r.ToRotationVector2().X > 0)
                {
                    player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
                }
                else
                {
                    player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D saw = this.getTextureAlt("Saw");
            Texture2D cs = CEUtils.getExtraTex("CircularSmear");
            Vector2 offset = CEUtils.randomPointInCircle((Hitted && Projectile.ai[1] == 0 && Counter < 36) || (Projectile.localAI[0] > 7 && Projectile.ai[1] != 0 && Counter < 60) ? 8 : 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + offset, null, lightColor * Projectile.Opacity, Projectile.rotation, tex.Size() * 0.5f + heldOrigin, Projectile.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            Main.spriteBatch.Draw(saw, sawOrigin - Main.screenPosition + offset, null, lightColor * Projectile.Opacity, Main.GameUpdateCount * -1.2f * dir, saw.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            if (!Hitted && sAlpha > 0)
            {
                Main.spriteBatch.UseAdditiveClamp();
                Main.spriteBatch.Draw(cs, Projectile.Center - Main.screenPosition, null, new Color(120, 255, 0) * 0.4f * sAlpha * Projectile.Opacity, Projectile.rotation + MathHelper.PiOver2 * 0.5f * dir, cs.Size() * 0.5f, Projectile.scale * 1f, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(sawOrigin, (Hitted ? 22 : 16) * Projectile.scale).Intersects(targetHitbox);
        }
        public int dir => (int)Projectile.ai[0] * (Projectile.velocity.X > 0 ? 1 : -1);
        public Vector2 heldOrigin => new Vector2(-26, 10 * dir);
        public Vector2 sawOrigin => Projectile.Center + (new Vector2(26, 0) - heldOrigin).RotatedBy(Projectile.rotation) * Projectile.scale;
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.boss)
                target.velocity *= 0.1f;
            if (this.Target == -1)
                this.Target = target.whoAmI;
            CalamityEntropy.Instance.screenShakeAmp = 1.2f;
            Hitted = true;
            target.AddBuff<MechanicalTrauma>(160);
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1.2f, 1.6f), target.Center, 4, CEUtils.WeapSound);
            for(int i = 0; i < 9; i++)
            {
                Vector2 pos = sawOrigin + (Projectile.rotation + MathHelper.PiOver2 * dir).ToRotationVector2() * 14 * Projectile.scale;
                Vector2 vel = Projectile.rotation.ToRotationVector2().RotatedByRandom(0.07f) * Main.rand.NextFloat(10, 70);
                Color color = Main.rand.NextBool() ? Color.Orange : Color.Firebrick;
                float scale = Main.rand.NextFloat(0.4f, 2.6f);
                if (Main.rand.NextBool())
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(pos, vel, false, Main.rand.Next(3, 9), scale, color));
                }
                else
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(pos, vel, false, Main.rand.Next(3, 9), scale, color));
                }
            }
        }
    }
    public class AzafureSawSpark : ModProjectile
    {
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += target.defense;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 16;
            Projectile.MaxUpdates = 5;
            Projectile.timeLeft = 50;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = Projectile.Center;
                    Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(16, 46);
                    Color color = Main.rand.NextBool() ? Color.Orange : Color.Firebrick;
                    float scale = Main.rand.NextFloat(1.8f, 2.6f);
                    if (Main.rand.NextBool())
                    {
                        GeneralParticleHandler.SpawnParticle(new LineParticle(pos, vel, false, Main.rand.Next(3, 6), scale, color));
                    }
                    else
                    {
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(pos, vel, false, Main.rand.Next(3, 6), scale, color));
                    }
                }
            }
            if (Projectile.localAI[0] == 2)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 pos = Projectile.Center;
                    Vector2 vel = Projectile.velocity * Main.rand.NextFloat(1f, 8.7f);
                    Color color = Main.rand.NextBool() ? Color.Orange : Color.Firebrick;
                    float scale = Main.rand.NextFloat(0.4f, 2.8f);
                    if (Main.rand.NextBool())
                    {
                        GeneralParticleHandler.SpawnParticle(new LineParticle(pos, vel, false, Main.rand.Next(5, 11), scale, color));
                    }
                    else
                    {
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(pos, vel, false, Main.rand.Next(5, 11), scale, color));
                    }
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(160);
            EParticle.spawnNew(new ShineParticle(), Projectile.Center, Vector2.Zero, new Color(255, 200, 30), 0.3f, 1, true, BlendState.Additive, 0, 8);
            EParticle.spawnNew(new ShineParticle(), Projectile.Center, Vector2.Zero, new Color(255, 255, 255), 0.18f, 1, true, BlendState.Additive, 0, 8);
            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(16, 46);
                Color color = Main.rand.NextBool() ? Color.Orange : Color.Firebrick;
                float scale = Main.rand.NextFloat(1.8f, 2.6f);
                if (Main.rand.NextBool())
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(pos, vel, false, Main.rand.Next(3, 6), scale, color));
                }
                else
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(pos, vel, false, Main.rand.Next(3, 6), scale, color));
                }
            }
            SoundStyle burn = new("CalamityMod/Sounds/Item/WeldingBurn");
            SoundEngine.PlaySound(burn with { Volume = 0.4f, Pitch = 0.55f }, target.Center);
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override bool PreDraw(ref Color c)
        {
            return false;
        }
    }
}
