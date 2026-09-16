using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 空间裂隙缝:以 Center 为中点、velocity 为方向、半长 ai[1] 的一条缝。
    /// 节拍:预告(ai[0] 帧,发丝白线越来越亮、两端张力刻线)→ 拉开(12 帧,判定窗;全屏滤镜沿线法向外推 + 色散)→
    /// 猛合(8 帧,两侧各喷一排垂直虚空弹)→ 消散。ai[2] = 喷弹伤害(已折算),取负则只向外侧喷(P3 六边形笼的边)。
    /// 缝完全可见后才开口、判定只在开口期:预告即承诺
    /// </summary>
    public class VDRiftSeam : VDHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => 120;

        public int AimFrames => (int)Math.Max(Projectile.ai[0], 6f);
        public float HalfLength => Projectile.ai[1] > 0 ? Projectile.ai[1] : VDDirector.RiftHalfLength;
        public int BoltDamage => (int)Math.Abs(Projectile.ai[2]);
        public bool OutwardOnly => Projectile.ai[2] < 0f;
        public Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        public Vector2 A => Projectile.Center - Dir * HalfLength;
        public Vector2 B => Projectile.Center + Dir * HalfLength;
        public int Age => (int)Projectile.localAI[1];
        public int TotalLife => AimFrames + VDDirector.RiftOpenFrames + VDDirector.RiftCloseFrames;

        /// <summary>开口量 0..1:拉开期快速张到 1,猛合期急收</summary>
        public float Openness {
            get {
                int t = Age - AimFrames;
                if (t < 0) {
                    return 0f;
                }
                if (t < VDDirector.RiftOpenFrames) {
                    float p = t / (float)VDDirector.RiftOpenFrames;
                    return 1f - MathF.Pow(1f - p, 3f);
                }
                float c = (t - VDDirector.RiftOpenFrames) / (float)VDDirector.RiftCloseFrames;
                return MathHelper.Clamp(1f - c * c, 0f, 1f);
            }
        }

        public bool IsOpen => Age >= AimFrames && Age < AimFrames + VDDirector.RiftOpenFrames;

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = TotalLife;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("VoidAnticipation", 1.35f, Projectile.Center, 4, 0.7f);
                }
            }
            Projectile.localAI[1]++;
            Projectile.rotation = Dir.ToRotation();
            int age = Age;

            //张力粒子:预告末段沿缝两侧被吸向缝线
            if (!Main.dedServ && age < AimFrames && age > AimFrames / 3 && Main.rand.NextBool(2)) {
                float along = Main.rand.NextFloat(-HalfLength, HalfLength);
                Vector2 side = Dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-90f, 90f);
                Vector2 pos = Projectile.Center + Dir * along + side;
                VDVfx.SparkBurst(pos, VDVfx.RiftWhite, 1, 0.1f, 0.2f, 12, 0.3f, 0.5f);
            }

            if (age == AimFrames) {
                //拉开:撕裂音 + 震屏 + 沿缝一排火花
                if (!Main.dedServ) {
                    CEUtils.PlaySound("VoidAttack", 1.4f, Projectile.Center, 4, 0.9f);
                    CEUtils.SetShake(Projectile.Center, 6f, 2200f);
                    for (int i = 0; i < 24; i++) {
                        Vector2 pos = Projectile.Center + Dir * Main.rand.NextFloat(-HalfLength, HalfLength);
                        Vector2 v = Dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-7f, 7f);
                        VDVfx.SparkBurst(pos, VDVfx.RiftWhite, 1, v.Length(), v.Length(), 18, 0.5f, 0.9f);
                    }
                }
            }

            if (age == AimFrames + VDDirector.RiftOpenFrames) {
                //猛合:两侧喷弹
                if (!Main.dedServ) {
                    CEUtils.PlaySound("VoidBomb", 1.3f, Projectile.Center, 4, 0.7f);
                    CEUtils.SetShake(Projectile.Center, 4f, 1800f);
                }
                if (IsServer && BoltDamage > 0) {
                    SpitBolts();
                }
            }

            //滤镜上报:开口期沿缝法向外推(纯本地)
            float open = Openness;
            if (open > 0.01f) {
                VDScreenFx.ReportRift(A, B, open);
                Lighting.AddLight(Projectile.Center, VDVfx.VoidPurple.ToVector3() * open);
            }
        }

        /// <summary>两侧各一排垂直虚空弹,沿缝均布;OutwardOnly 时只向远离玩家的一侧(笼边)喷</summary>
        private void SpitBolts() {
            Vector2 normal = Dir.RotatedBy(MathHelper.PiOver2);
            int count = VDDirector.RiftBoltsPerSide;
            float towardPlayer = 0f;
            if (OutwardOnly) {
                Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                towardPlayer = Math.Sign(Vector2.Dot(closest.Center - Projectile.Center, normal));
            }
            for (int i = 0; i < count; i++) {
                float along = MathHelper.Lerp(-HalfLength * 0.85f, HalfLength * 0.85f, (i + 0.5f) / count);
                Vector2 pos = Projectile.Center + Dir * along;
                for (int side = -1; side <= 1; side += 2) {
                    if (OutwardOnly && side == towardPlayer) {
                        continue;
                    }
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, normal * side * VDDirector.RiftBoltSpeed, ModContent.ProjectileType<VDVoidBolt>(), BoltDamage, 0f, Main.myPlayer, VDVoidBolt.ModeStraight);
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!IsOpen) {
                return false;
            }
            return CEUtils.LineThroughRect(A, B, targetHitbox, (int)VDDirector.RiftWidth);
        }

        public override bool PreDraw(ref Color lightColor) {
            int age = Age;
            Vector2 a = A;
            Vector2 b = B;
            float open = Openness;
            Main.spriteBatch.UseAdditive();
            if (age < AimFrames) {
                //发丝白线:越近开口越亮,末 6 帧闪烁;两端张力刻线
                float p = age / (float)AimFrames;
                float flicker = AimFrames - age <= 6 ? 0.6f + 0.4f * (float)Math.Sin(age * 1.6f) : 1f;
                Color c = VDVfx.RiftWhite * (0.25f + 0.7f * p) * flicker;
                CEUtils.drawLine(a, b, c, 1.5f + p);
                CEUtils.drawLineBetter(a, b, VDVfx.VoidPurple * (0.2f * p), 6f + 6f * p);
                Vector2 tick = Dir.RotatedBy(MathHelper.PiOver2) * (6f + 10f * p);
                CEUtils.drawLine(a - tick, a + tick, c, 1.5f);
                CEUtils.drawLine(b - tick, b + tick, c, 1.5f);
            }
            CEUtils.ReSetToEndShader();

            if (open > 0.01f) {
                //缝体:白紫光缝,宽度随开口
                float width = 10f + 34f * open;
                VDBeamDraw.Draw(a, Dir, HalfLength * 2f, width, VDVfx.VoidPurple, VDVfx.RiftWhite, open, 1f, Projectile.whoAmI * 0.53f);
                Main.spriteBatch.UseAdditive();
                CEUtils.drawLine(a, b, Color.White * (0.9f * open), 2f + 2f * open);
                CEUtils.ReSetToEndShader();
            }
            return false;
        }
    }
}
