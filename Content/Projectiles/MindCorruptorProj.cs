using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class MindCorruptorProj : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 4;
            this.segments = 25;
            this.rangeMult = 3.6f;
            Projectile.localNPCHitCooldown = 16;
        }
        public static float segRangeMult = 1.7f;
        public bool playSound = true;
        public Vector2 LastEnd = Vector2.Zero;
        public int hookNPC = -1;
        public int hookTime = 100;
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(hookNPC);
            writer.Write(hookTime);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            hookNPC = reader.ReadInt32();
            hookTime = reader.ReadInt32();
        }
        public override string getTagEffectName => "MindCorruptor";
        public override SoundStyle? WhipSound => null;
        public override void WhipAI() {
            base.WhipAI();
            if (playSound) {
                Projectile.localAI[0] = Main.rand.NextFloat(-1, 1);
                playSound = false;
                SoundEngine.PlaySound(SoundID.Item152, Projectile.Center);
            }
            if (Main.rand.Next(4) <= 1) {
                Dust dust7 = Dust.NewDustDirect(EndPoint - new Vector2(4, 4), 8, 8, DustID.CorruptTorch, 0f, 0f, 0, new Color(130, 70, 160), 1.3f);
                if (LastEnd != Vector2.Zero) {
                    dust7.velocity = EndPoint - LastEnd;
                }
                dust7.velocity *= Main.rand.NextFloat() * 0.8f;
                dust7.noGravity = true;
                dust7.scale = 0.9f + Main.rand.NextFloat() * 0.9f;
                dust7.fadeIn = Main.rand.NextFloat() * 0.9f;
            }

            if (hookNPC > -1) {
                if (hookTime > 1) {
                    if (!hookNPC.ToNPC().active) {
                        hookTime = 1;
                    }
                }
            }
            if (Projectile.localAI[2] == 0) {
                LastEnd = EndPoint;
            }
            if (Projectile.localAI[2]++ % 2 == 0) {
                if (hookNPC == -1 || hookTime <= 0) {
                    Vector2 top = EndPoint;
                    Vector2 sparkVelocity2 = (EndPoint - LastEnd) * 0.6f;
                    int sparkLifetime2 = Main.rand.Next(10, 14);
                    float sparkScale2 = Main.rand.NextFloat(1f, 1.2f);
                    Color sparkColor2 = Color.Lerp(Color.DarkBlue, Color.Purple, Main.rand.NextFloat(0, 1));
                    //AltSpark跟LineCal随机混用,旧Calamity spark/Lines二选一
                    PRTLoader.NewParticle<PRT_AltSpark>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));

                    sparkScale2 = Main.rand.NextFloat(0.3f, 0.5f);
                    sparkColor2 = Color.Lerp(Color.Aqua, new Color(200, 200, 255), Main.rand.NextFloat(0, 1));
                    PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                }
                LastEnd = EndPoint;
            }
            if (hookNPC > -1 && hookTime > 0) {
                hookTime--;
                if (hookTime == 0) {
                    hookTime--;
                    HNPCPos = hookNPC.ToNPC().Center;
                    CEUtils.PlaySound("corruptwhip_hit", 1, hookNPC.ToNPC().Center);
                    for (float i = 0; i <= 1; i += 0.05f) {
                        //LargeBloom/FlameExplosion那些Calamity路径字符串原样保留
                        //CustomPulse贴图路径现传,走PRTPathTextures缓存,Configure第一个string是TexPath
                        PRTLoader.NewParticle<PRT_CustomPulse>(hookNPC.ToNPC().Center, Vector2.Zero, Color.Lerp(Color.LightBlue, Color.Purple, i) * 0.8f, 0.01f).Configure("CalamityEntropy/Assets/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.01f, i * 0.14f, (int)((1.2f - i) * 20));
                    }
                    //换用自有屏震系统, 幅度对齐同类爆点(原灾厄震屏强度8)
                    CalamityEntropy.Instance.screenShakeAmp = 3;
                }
                Projectile.ai[0] = getFlyTime() * Projectile.MaxUpdates * 0.5f;
                Projectile.GetOwner().itemAnimation = Projectile.GetOwner().itemTime = Projectile.GetOwner().itemTime / 2;

            }
            Player player = Projectile.GetOwner();
            if (player.itemTime < 2) {
                player.itemAnimation = player.itemTime = 2;
            }
            if (hookNPC > -1 && hookTime <= 0) {
                Projectile.ai[0] += 2;
            }
        }
        public override Color StringColor => Color.DarkBlue;
        public Vector2 HNPCPos = Vector2.Zero;

        public List<Vector2> getPoints(float p) {
            Projectile.GetWhipSettings(Projectile, out float ttfo, out int segs, out rangeMult);
            if (hookNPC >= 0 && hookTime > 0) {
                List<Vector2> ps = new List<Vector2>();
                for (int i = 0; i <= segs; i++) {
                    ps.Add(Vector2.Lerp(Projectile.GetOwner().HandPosition.Value, hookNPC.ToNPC().Center, (float)i / segs));
                }
                return ps;
            }
            if (p > 0.5f && HNPCPos != Vector2.Zero) {
                List<Vector2> ps = new List<Vector2>();
                for (int i = 0; i <= segs; i++) {
                    ps.Add(Vector2.Lerp(Projectile.GetOwner().HandPosition.Value, Vector2.Lerp(HNPCPos, Projectile.GetOwner().HandPosition.Value, (p - 0.5f) * 2), (float)i / segs) + Projectile.velocity.RotatedBy(MathHelper.PiOver2).normalize() * (float)Math.Cos(Main.GameUpdateCount * 0.2f + i * 0.4f) * ((p - 0.5f) * 2) * 64 * (float)i / segs);
                }
                return ps;
            }
            rangeMult *= 2;
            rangeMult *= Projectile.GetOwner().whipRangeMultiplier;
            List<Vector2> points = new List<Vector2>();
            float pc = ((float)Math.Cos(p * MathHelper.Pi - MathHelper.PiOver2)) * 0.5f;
            Vector2 start = Projectile.GetOwner().HandPosition.Value;
            Vector2 mid = start + Projectile.velocity * rangeMult * 46 * pc;
            Vector2 end = start + Projectile.velocity * rangeMult * 46 * pc + (Projectile.velocity * pc * 82 * rangeMult).RotatedBy(Projectile.localAI[0] > 0 ? p * -MathHelper.TwoPi + MathHelper.Pi : p * MathHelper.TwoPi - MathHelper.Pi);
            Vector2 c = end.ClosestPointOnLine(Projectile.GetOwner().HandPosition.Value, Projectile.GetOwner().HandPosition.Value + Projectile.velocity * 128);
            end = c + (end - c) * (Math.Abs(Projectile.localAI[0]) * 0.18f);
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
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            if (hookNPC == -1) {
                hookNPC = target.whoAmI;
            }
            Projectile.netUpdate = true;
            CEUtils.PlaySound("corruptwhip_hit2", 1, target.Center);
            target.AddBuff(ModContent.BuffType<MindCorruptorWhipDebuff>(), 240);
        }
        public override bool? CanHitNPC(NPC target) {
            if (hookTime > 2 && hookNPC > -1) {
                return false;
            }
            return base.CanHitNPC(target);
        }
        public override int handleHeight => 28;
        public override int segHeight => 20;
        public override int endHeight => 26;
        public override int segTypes => 3;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (hookTime > 6) {
                modifiers.SourceDamage *= 0.2f;
            }
        }
    }
    public class CorruptStrike : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 10;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI() {
            if (Projectile.ai[0] == 0) {
                Vector2 top = Projectile.Center;
                Vector2 sparkVelocity2 = Projectile.velocity * 1.26f;
                int sparkLifetime2 = Main.rand.Next(12, 16);
                float sparkScale2 = Main.rand.NextFloat(1.6f, 1.8f);
                Color sparkColor2 = Color.Lerp(Color.Purple, Color.Black, Main.rand.NextFloat(0.4f, 1));
                PRTLoader.NewParticle<PRT_AltSpark>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));

                sparkScale2 = Main.rand.NextFloat(1.4f, 1.6f);
                sparkColor2 = Color.Lerp(Color.MediumPurple, Color.Blue, Main.rand.NextFloat(0, 1));
                var spark2 = PRTLoader.NewParticle<PRT_GlowSparkCal>(top, sparkVelocity2, sparkColor2, sparkScale2 * 0.06f).Configure(false, (int)(sparkLifetime2), new Vector2(0.25f, 1));
                spark2.Glowing = false;
            }
            ++Projectile.ai[0];
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("slice", Main.rand.NextFloat(0.6f, 1.4f), target.Center, 8, volume: 0.7f);
        }
    }
}
