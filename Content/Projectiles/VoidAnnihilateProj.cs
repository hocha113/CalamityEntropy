using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VoidAnnihilateSpawner : ModProjectile
    {
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
        }
        public override bool? CanDamage() => false;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if(!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }
            float speed = player.GetWeaponAttackSpeed(player.HeldItem);
            player.itemTime = player.itemAnimation = 2;
            Projectile.ai[0]++;
            Projectile.StickToPlayer();
            int stime = 8;
            player.SetHandRot(Projectile.rotation);
            if (Projectile.ai[0] >= Projectile.ai[1] * (stime / speed))
            {
                if (Projectile.ai[1] <= 2)
                {
                    Projectile.ai[0] = Projectile.ai[1] * (stime / speed);
                    Projectile.ai[1]++;
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (Main.MouseWorld - Projectile.Center).normalize() * Projectile.velocity.Length(), ModContent.ProjectileType<VoidAnnihilateProj>(), Projectile.damage, Projectile.knockBack, player.whoAmI);
                    }
                }
            }
            if (Projectile.ai[0] > 40f / speed)
                Projectile.Kill();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
    public class VoidAnnihilateProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 16000;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 160;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }
        public bool Hitted { get { return Projectile.ai[0] > 0; } set { Projectile.ai[0] = 1; } }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void AI()
        {
            CEUtils.AddLight(Projectile.Center, new Color(90, 90, 255));
            if (Projectile.Entropy().FirstFrames)
                CEUtils.PlaySound("HammerShoot" + Main.rand.Next(1, 4), Main.rand.NextFloat(2.2f, 2.5f), Projectile.Center, 12, 0.6f);
            if (Hitted)
                if (ForceStrike)
                {
                    if (targetNPC == null)
                    {
                        ForceStrike = false;
                        Projectile.ai[2] = -1;
                        return;
                    }
                    if (Projectile.localAI[0] == 0)
                    {
                        Projectile.velocity += (Projectile.Center - targetNPC.Center).normalize() * 32;
                    }
                    Projectile.velocity *= 0.9f;
                    if (Projectile.localAI[2]++ > 16)
                        Projectile.velocity += (targetNPC.Center - Projectile.Center).normalize() * float.Min(Projectile.localAI[0] * 0.44f, 12);
                    else
                        Projectile.velocity *= 0.9f;
                    Projectile.localAI[0]++;
                }
                else
                {
                    Projectile.rotation += Projectile.velocity.Length() * 0.05f * (Projectile.whoAmI % 2 == 0 ? 1 : -1);
                    Projectile.velocity *= 0.95f;
                }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            oldPos.Add(Projectile.Center - Projectile.velocity * 0.5f);
            if (oldPos.Count > 10)
            {
                oldPos.RemoveAt(0);
            }
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 10)
            {
                oldPos.RemoveAt(0);
            }
            Projectile.localAI[1] = float.Lerp(Projectile.localAI[1], ForceStrike ? 1 : 0, 0.2f);
        }
        public bool ForceStrike { get { return Projectile.ai[1] > 0; } set { Projectile.ai[1] = 1; } }
        public NPC targetNPC
        {
            get
            {
                if (!ForceStrike)
                    return null;
                NPC n = ((int)Projectile.ai[2]).ToNPC();
                if (!n.active)
                    return null;
                return n;
            }
            set
            {
                Projectile.ai[2] = value.whoAmI;
            }
        }    
        
        public override bool? CanDamage()
        {
            if (Hitted && !ForceStrike)
                return false;
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Hitted)
            {
                Hitted = true;
                Projectile.timeLeft = 900;
                CEUtils.SyncProj(Projectile.whoAmI);
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(140, 140, 255), "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 1.8f, 15));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(80, 80, 255), "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 1.4f, 18));
                for (int i = 0; i < 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(2f, 2.5f);
                    dust.velocity = (new Vector2(35, 35).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                    dust.noGravity = false;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }
                CEUtils.PlaySound("DemonSwordImpact2", Main.rand.NextFloat(1.6f, 2f), Projectile.Center, 8, 0.8f);
                Projectile.velocity = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(17, 24);
                int s = 0;
                foreach(Projectile p in Main.ActiveProjectiles)
                {
                    if (p.type == Projectile.type && p.owner == Projectile.owner && p.ai[0] > 0)
                        s++;
                }
                CEUtils.SyncProj(Projectile.whoAmI);
                if (s > 15)
                    Projectile.Kill();
            }
            if(ForceStrike)
            {
                CEUtils.PlaySound("moonlighthit" + Main.rand.Next(2), Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, 6, 1);
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(2f, 2.5f);
                    dust.velocity = (new Vector2(35, 35).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                    dust.noGravity = false;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(180, 180, 255), "CalamityMod/Particles/GlowSquareParticleThick", Vector2.One, CEUtils.randomRot(), 0.005f, 1f, 16));
                Projectile.Kill();
            }
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int t = reader.ReadInt32();
            if (t > 10)
                Projectile.timeLeft = t;
        }
        public override void OnKill(int timeLeft)
        {
            CEUtils.PlaySound("DemonSwordImpact2", Main.rand.NextFloat(1.2f, 1.4f), Projectile.Center, 8, 0.8f);

            float scale = 2.6f;
            if (!ForceStrike)
            {
                for (int i = 0; i < 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(2f, 2.5f);
                    dust.velocity = (new Vector2(35, 35).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                    dust.noGravity = false;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(140, 140, 255), "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(140, 140, 255), "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18));
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float trailOffset = Main.GlobalTimeWrappedHourly * 4;
            if(ForceStrike)
            {
                if (targetNPC != null)
                {
                    Vector2 pos = targetNPC.Center;
                    Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);

                    float alpha = Projectile.localAI[1];
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<ColoredVertex> ve2 = new List<ColoredVertex>();
                    for (float i = 0; i <= 1; i += 0.02f)
                    {
                        Vector2 m = Vector2.Lerp(Projectile.Center, pos, i);
                        Vector2 l = (Projectile.Center - pos).normalize().RotatedBy(MathHelper.PiOver2);
                        ve.Add(new ColoredVertex(m - l * alpha * 24 - Main.screenPosition,
                              new Vector3(i * 4 - trailOffset, 1, 1),
                              new Color(60, 60, 255) * alpha));
                        ve.Add(new ColoredVertex(m + l * alpha * 24 - Main.screenPosition,
                              new Vector3(i * 4 - trailOffset, 0, 1),
                              new Color(60, 60, 255) * alpha));

                        ve2.Add(new ColoredVertex(m - l * alpha * 6 - Main.screenPosition,
                              new Vector3(i * 4 - trailOffset, 1, 1),
                              new Color(255, 255, 255) * alpha));
                        ve2.Add(new ColoredVertex(m + l * alpha * 6 - Main.screenPosition,
                              new Vector3(i * 4 - trailOffset, 0, 1),
                              new Color(255, 255, 255) * alpha));
                    }
                    if (ve.Count >= 3)
                    {
                        Texture2D tx = CEUtils.getExtraTex("Streak1");
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
                    }
                    CEUtils.DrawGlow(pos, new Color(20, 20, 255), 1f * Projectile.localAI[1], true, null, false);
                    CEUtils.DrawGlow(pos, new Color(255, 255, 255), 0.6f * Projectile.localAI[1], true, null, false);
                    CEUtils.DrawGlow(Projectile.Center, new Color(20, 20, 255), 2.3f * Projectile.localAI[1], true, null, false);
                    CEUtils.DrawGlow(Projectile.Center, new Color(255, 255, 255), 1.8f * Projectile.localAI[1], true, null, false);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }
            else if (!Hitted && oldPos.Count > 1)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
                float alpha = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<ColoredVertex> ve2 = new List<ColoredVertex>();
                oldPos.Add(Projectile.Center);
                for (int i = 0; i < oldPos.Count; i++)
                {
                    alpha = i / (oldPos.Count - 1f);
                    Vector2 m = oldPos[i];
                    Vector2 l = i == 0 ? Vector2.Zero : (oldPos[i] - oldPos[i - 1]).normalize().RotatedBy(MathHelper.PiOver2);
                    ve.Add(new ColoredVertex(m + l * alpha * 15 - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 1, 1),
                          new Color(60, 60, 255) * alpha));
                    ve.Add(new ColoredVertex(m - l * alpha * 15 - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 0, 1),
                          new Color(60, 60, 255) * alpha));

                    ve2.Add(new ColoredVertex(m + l * alpha * 12 - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 1, 1),
                          new Color(255, 255, 255) * alpha));
                    ve2.Add(new ColoredVertex(m - l * alpha * 12 - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 0, 1),
                          new Color(255, 255, 255) * alpha));
                }
                oldPos.RemoveAt(oldPos.Count - 1);
                if (ve.Count >= 3)
                {
                    Texture2D tx = CEUtils.getExtraTex("Streak2");
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Texture2D tx2 = CEUtils.getExtraTex("Streak1");
                    gd.Textures[0] = tx2;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tex = Projectile.GetTexture();
            if(Hitted)
            {
                Main.spriteBatch.UseAdditive();
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
                {
                    Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + (i + Main.GlobalTimeWrappedHourly * 12).ToRotationVector2() * 4, null, Color.White, Projectile.rotation - MathHelper.ToRadians(30), tex.Size().Half(), Projectile.scale, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation - MathHelper.ToRadians(30), tex.Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
    public class VoidAnnihilateCharge : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 16000;
        }
        public override string Texture => "CalamityEntropy/Content/Projectiles/VoidAnnihilateProj";
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            Projectile.ai[2] *= 0.7f;
            Player player = Projectile.GetOwner();
            CEUtils.AddLight(Projectile.Center, new Color(90, 90, 255));
            if (Projectile.ai[1] <= 0)
            {
                if (!player.active || player.dead)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.timeLeft = 180;
                float speed = player.GetWeaponAttackSpeed(player.HeldItem);
                player.itemTime = player.itemAnimation = 2;
                Projectile.StickToPlayer();
                int dir = (player.Calamity().mouseWorld.X - player.Center.X) > 0 ? 1 : -1;
                Projectile.rotation += dir * -2.2f * CEUtils.CustomLerp1(Projectile.ai[0]);
                player.SetHandRotWithDir(Projectile.rotation, dir);
                Projectile.Center += Projectile.rotation.ToRotationVector2() * 26 * Projectile.scale;
                if (Projectile.ai[0] < 1)
                {
                    Projectile.ai[0] += 0.025f * speed;
                    if (Projectile.ai[0] >= 1)
                    {
                        if (Projectile.localAI[1]++ == 0)
                        {
                            CEUtils.PlaySound("bne_hit2", Main.rand.NextFloat(0.4f, 0.6f), Projectile.Center);
                            Projectile.ai[2] = 8;
                            Projectile.ai[0] = 1;

                            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center + (Projectile.rotation + dir * 0.9f).ToRotationVector2() * 13, Vector2.Zero, new Color(120, 120, 255), "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 0.8f, 10) { Velocity = player.velocity});
                        }
                    }
                }
                Projectile.scale = 1 + Projectile.ai[0] * 0.5f;
                if (!player.channel)
                {
                    if (Projectile.ai[0] >= 1)
                    {
                        Projectile.ai[1] = 1;
                        Projectile.MaxUpdates *= 2;
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((player.Calamity().mouseWorld - Projectile.Center).ToRotation());
                        CEUtils.PlaySound("runesong3", Main.rand.NextFloat(0.8f, 1f), Projectile.Center, 10);
                        Projectile.rotation = Projectile.velocity.ToRotation();
                    }
                    else
                    {
                        if (Main.myPlayer == Projectile.owner)
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<VoidAnnihilateSpawner>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        Projectile.Kill();
                    }
                }
            }
            else
            {
                oldPos.Add(Projectile.Center);
                if (oldPos.Count > 15)
                {
                    oldPos.RemoveAt(0);
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
        }
        public override bool? CanDamage()
        {
            return Projectile.ai[1] > 0;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int t = ModContent.ProjectileType<VoidAnnihilateProj>();
            int count = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == Projectile.owner && p.type == t)
                    if (p.ModProjectile is VoidAnnihilateProj va)
                    {
                        if (!va.ForceStrike && va.Hitted)
                        {
                            count++;
                            va.ForceStrike = true;
                            va.targetNPC = target;
                            CEUtils.SyncProj(p.whoAmI);
                        }
                    }
            }
            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, (count > 0 ? 13f : 4f) * 62, Projectile.DamageType);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(160, 160, 255), "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, count > 0 ? 10f : 4f, 24));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(90, 90, 255), "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, count > 0 ? 5f : 3.2f, 28));

            Projectile.Kill();
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 2;
        }
        public override bool ShouldUpdatePosition()
        {
            return Projectile.ai[1] == 1;
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[1] == 0)
                return;
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(90, 90, 255), "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 2f, 19));
            CEUtils.PlaySound("DemonSwordInsaneImpact", Main.rand.NextFloat(1.8f, 2f), Projectile.Center, 8, 0.16f);
            CEUtils.PlaySound("Smash" + Main.rand.Next(1, 3), Main.rand.NextFloat(1.2f, 1.5f), Projectile.Center);
            CEUtils.PlaySound("Smash" + Main.rand.Next(1, 3), Main.rand.NextFloat(1.2f, 1.5f), Projectile.Center);
            CEUtils.PlaySound("xhit", Main.rand.NextFloat(1.6f, 1.9f), Projectile.Center, 6, 0.8f);
            for (int i = 0; i < 36; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(3f, 3.5f);
                dust.velocity = (new Vector2(35, 35).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.8f, 1.4f);
                dust.noGravity = false;
                dust.color = Color.LightBlue;
                dust.fadeIn = 2f;
            }
            float scale = 4f;
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(200, 200, 255), "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(200, 200, 255), "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 14));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(200, 200, 255), "CalamityMod/Particles/PulseStar", Vector2.One, 0, 0.005f, scale * 0.035f, 18));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float trailOffset = Main.GlobalTimeWrappedHourly * 4;
            if (Projectile.ai[1] >= 1 && oldPos.Count > 1)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
                float alpha = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<ColoredVertex> ve2 = new List<ColoredVertex>();
                oldPos.Add(Projectile.Center);
                for (int i = 0; i < oldPos.Count; i++)
                {
                    alpha = i / (oldPos.Count - 1f);
                    Vector2 m = oldPos[i];
                    Vector2 l = i == 0 ? Vector2.Zero : (oldPos[i] - oldPos[i - 1]).normalize().RotatedBy(MathHelper.PiOver2);
                    ve.Add(new ColoredVertex(m + l * alpha * 20 * Projectile.scale - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 1, 1),
                          new Color(80, 80, 255) * alpha));
                    ve.Add(new ColoredVertex(m - l * alpha * 20 * Projectile.scale - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 0, 1),
                          new Color(80, 80, 255) * alpha));

                    ve2.Add(new ColoredVertex(m + l * alpha * 12 * Projectile.scale - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 1, 1),
                          new Color(255, 255, 255) * alpha));
                    ve2.Add(new ColoredVertex(m - l * alpha * 12 * Projectile.scale - Main.screenPosition,
                          new Vector3(alpha * 2 + trailOffset, 0, 1),
                          new Color(255, 255, 255) * alpha));
                }
                oldPos.RemoveAt(oldPos.Count - 1);
                if (ve.Count >= 3)
                {
                    Texture2D tx = CEUtils.getExtraTex("Streak2");
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Texture2D tx2 = CEUtils.getExtraTex("Streak1");
                    gd.Textures[0] = tx2;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseAdditive();
            for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
            {
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + (i + Main.GlobalTimeWrappedHourly * 12).ToRotationVector2() * (Projectile.ai[0] * 6 + Projectile.ai[2]), null, Color.White * (Projectile.ai[0] >= 1 ? 0.66f : 0.5f), Projectile.rotation - MathHelper.ToRadians(30), tex.Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation - MathHelper.ToRadians(30), tex.Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
