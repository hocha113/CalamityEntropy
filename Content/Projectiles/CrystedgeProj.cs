using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class CrystedgeProj : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 6;
            this.segments = 26;
            this.rangeMult = 1.2f;
        }
        public static float segRangeMult = 1.7f;
        public bool playSound = true;
        public Vector2 LastEnd = Vector2.Zero;
        public override string getTagEffectName => "Crystedge";
        public override SoundStyle? WhipSound => new SoundStyle("CalamityEntropy/Assets/Sounds/crystalsound" + Main.rand.Next(1, 3));
        public override void WhipAI() {
            base.WhipAI();
            if (playSound) {
                Projectile.localAI[0] = Main.rand.NextFloat(-1, 1);
                playSound = false;
                SoundEngine.PlaySound(SoundID.Item152, Projectile.Center);
            }
            if (Main.rand.Next(4) <= 1) {
                Dust dust7 = Dust.NewDustDirect(EndPoint - new Vector2(4, 4), 8, 8, DustID.RainbowMk2, 0f, 0f, 0, new Color(130, 70, 160), 1.3f);
                if (LastEnd != Vector2.Zero) {
                    dust7.velocity = EndPoint - LastEnd;
                }
                dust7.velocity *= Main.rand.NextFloat() * 0.8f;
                dust7.noGravity = true;
                dust7.scale = 0.9f + Main.rand.NextFloat() * 0.9f;
                dust7.fadeIn = Main.rand.NextFloat() * 0.9f;
            }
            LastEnd = EndPoint;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(ModContent.BuffType<CrystedgeWhipDebuff>(), 240);
        }
        public override void ModifyWhipSettings(ref float ttfo, ref int segs, ref float rangeMul) {
            ttfo = getFlyTime() * Projectile.MaxUpdates;
            if (Projectile.ai[2] == 1) {
                rangeMul *= segRangeMult;
            }
        }
        public override int getFlyTime() {
            return (int)(base.getFlyTime() * (Projectile.ai[2] == 1 ? 0.4f : 0.22f));
        }
        public override float getSegScale(int segCount, int segCounts) {
            return base.getSegScale(segCount, segCounts) * (Projectile.ai[2] == 1 ? 1 : 1f / segRangeMult);
        }
        public List<Vector2> getPoints(float p) {
            Projectile.GetWhipSettings(Projectile, out float ttfo, out int segs, out rangeMult);
            rangeMult *= Projectile.GetOwner().whipRangeMultiplier;
            List<Vector2> points = new List<Vector2>();
            float pc = ((float)Math.Cos(p * MathHelper.Pi - MathHelper.PiOver2)) * 0.5f;
            Vector2 start = Projectile.GetOwner().HandPosition.Value;
            Vector2 mid = start + Projectile.velocity * rangeMult * 46 * pc;
            Vector2 end = start + Projectile.velocity * rangeMult * 46 * pc + (Projectile.velocity * pc * 82 * rangeMult).RotatedBy(Projectile.localAI[0] > 0 ? p * -MathHelper.TwoPi + MathHelper.Pi : p * MathHelper.TwoPi - MathHelper.Pi);
            Vector2 c = end.ClosestPointOnLine(Projectile.GetOwner().HandPosition.Value, Projectile.GetOwner().HandPosition.Value + Projectile.velocity * 128);
            end = c + (end - c) * (Math.Abs(Projectile.localAI[0]) * 0.54f);
            for (int i = 0; i <= segs; i++) {
                points.Add(CEUtils.Bezier(new List<Vector2> { start, mid, end }, ((float)i / (float)segs)));
            }
            return points;
        }
        public override void ModifyControlPoints(List<Vector2> points) {
            points.Clear();
            List<Vector2> p2 = getPoints(Projectile.ai[0] / (this.getFlyTime() * Projectile.MaxUpdates));

            foreach (var p in p2) {
                points.Add(p);
            }
        }
        public override int handleHeight => 28;
        public override int segHeight => 26;
        public override int endHeight => 26;
        public override int segTypes => 2;
    }
    public class CrystedgeCrystalBig : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
        }
        public override void OnSpawn(IEntitySource source) {
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TrueExcalibur, new ParticleOrchestraSettings {
                PositionInWorld = Projectile.Center,
                MovementVector = Vector2.Zero
            });
            CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.7f, 1.3f), Projectile.Center);
        }
        public override void AI() {
            Projectile.velocity *= 0.98f;
            Projectile.rotation += 0.06f * Projectile.velocity.Length() * (Projectile.whoAmI % 2 == 0 ? 1 : -1);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }

        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            for (int i = 0; i < 2; i++) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-5, 5), ModContent.ProjectileType<CrystedgeCrystalMid>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
            }
            for (int i = 0; i < 14; i++) {
                Dust.NewDust(Projectile.Center, 1, 1, DustID.PinkCrystalShard, Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4));
            }
        }
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.timeLeft > 90) {
                return false;
            }
            return base.CanHitNPC(target);
        }
    }
    public class CrystedgeCrystalMid : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 120;
            Projectile.light = 0.3f;
        }
        public override void AI() {
            Projectile.velocity *= 0.98f;
            Projectile.rotation += 0.06f * Projectile.velocity.Length() * (Projectile.whoAmI % 2 == 0 ? 1 : -1);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, ModContent.Request<Texture2D>("CalamityEntropy/Content/Projectiles/CrystedgeCrystalMid" + (Projectile.whoAmI % 2 == 0 ? "1" : "")).Value));
            return false;
        }
        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            for (int i = 0; i < 3; i++) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-5, 5), ModContent.ProjectileType<CrystedgeCrystalSmall>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
            }
            for (int i = 0; i < 8; i++) {
                Dust.NewDust(Projectile.Center, 1, 1, DustID.PinkCrystalShard, Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4));
            }
        }

        public override bool? CanHitNPC(NPC target) {
            if (Projectile.timeLeft > 90) {
                return false;
            }
            return base.CanHitNPC(target);
        }
    }
    public class CrystedgeCrystalSmall : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 120;
            Projectile.light = 0.1f;
        }
        public override void AI() {
            Projectile.velocity *= 0.98f;
            Projectile.rotation += 0.06f * Projectile.velocity.Length() * (Projectile.whoAmI % 2 == 0 ? 1 : -1);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }

        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            for (int i = 0; i < 3; i++) {
                Dust.NewDust(Projectile.Center, 1, 1, DustID.PinkCrystalShard, Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4));
            }
        }
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.timeLeft > 90) {
                return false;
            }
            return base.CanHitNPC(target);
        }
    }
}