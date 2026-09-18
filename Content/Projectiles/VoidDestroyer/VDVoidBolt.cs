using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 虚空弹:驱逐舰通用紫色水晶弹,命中 348 + 虚空之火 3 秒。
    /// ai[0] 选行为模式:0 直飞;1 弧形包裹(ai[1]/ai[2] 为固定目标点);2 扇形后转向上加速(ai[1] 为转向延迟);
    /// 3 冲刺路径遗留(沿初速方向持续加速);4 形状弹幕(直飞轻绘制、短寿命);
    /// 5 纵深贯穿(远处生成,越来越大地穿过平面命中,再掠过镜头);6 越肩(镜头后生成,缩小着落到平面命中,再遁入深处);
    /// 7 纵深回旋(平面抛入深处,在 ai[1]/ai[2] 锁定点上空回头,穿过平面命中,再掠过镜头);
    /// 8 三维螺旋(绕 ai[1]/ai[2] 的倾斜轨道螺旋外扩,Z = 振幅 × sin 角,一圈两次穿过平面才有判定;奇点的吸积盘弹)。
    /// 深度模式的 Z / Z 速度 / Z 加速度由生成方经 <see cref="VDDepthSource"/> 给,平面速度由生成方按到达平面的帧数配好;螺旋模式自己算 Z
    /// </summary>
    public class VDVoidBolt : VDDepthProjectile
    {
        public const int ModeStraight = 0;
        public const int ModeArcWrap = 1;
        public const int ModeFanRise = 2;
        public const int ModeDashTrail = 3;
        public const int ModeShapeBurst = 4;
        public const int ModeZPierce = 5;
        public const int ModeZFromNear = 6;
        public const int ModeZBoomerang = 7;
        public const int ModeZOrbit = 8;
        /// <summary>螺旋弹寿命:约 1.7 圈、三四次穿越平面,奇点塌缩前基本散尽</summary>
        public const int OrbitLife = 120;

        public static readonly Color GlowColor = new Color(190, 60, 255);

        public int Mode => (int)Projectile.ai[0];
        public bool IsDepthMode => Mode >= ModeZPierce;
        /// <summary>螺旋弹的 Z 在正负之间来回,不要在「逼近」段画落点标记(一圈两次会闪两次),留给穿越那一瞬的判定说话</summary>
        public override bool WantsMarker => Mode != ModeZOrbit && base.WantsMarker;
        protected override float WhooshStrength => Mode == ModeZOrbit ? 0f : 0.35f;
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => 240;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 22;
            Projectile.height = 22;
        }

        protected override void DepthAI() {
            //首帧按模式定寿命(生成后改 timeLeft 不走同步,放在全端都会跑的首帧里)
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (Mode == ModeShapeBurst) {
                    Projectile.timeLeft = 120;
                }
                else if (Mode == ModeDashTrail) {
                    Projectile.timeLeft = 180;
                }
                else if (Mode == ModeZOrbit) {
                    Projectile.timeLeft = OrbitLife;
                    //起始角由生成位置相对轨道心反推(各端同一生成包,算得一样)
                    Projectile.localAI[2] = (Projectile.Center - new Vector2(Projectile.ai[1], Projectile.ai[2])).ToRotation();
                }
                else if (IsDepthMode) {
                    Projectile.timeLeft = 360;
                }
            }
            Projectile.localAI[1]++;
            float age = Projectile.localAI[1];

            switch (Mode) {
                case ModeArcWrap: {
                    Vector2 target = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                    Vector2 toTarget = target - Projectile.Center;
                    if (age < 120f && toTarget.Length() > 60f) {
                        float cur = Projectile.velocity.ToRotation();
                        float want = toTarget.ToRotation();
                        float diff = MathHelper.WrapAngle(want - cur);
                        float turn = MathHelper.Clamp(diff, -0.038f, 0.038f);
                        float speed = Math.Min(Projectile.velocity.Length() + 0.06f, 17f);
                        Projectile.velocity = (cur + turn).ToRotationVector2() * speed;
                    }
                    break;
                }
                case ModeFanRise: {
                    if (age > Projectile.ai[1]) {
                        float cur = Projectile.velocity.ToRotation();
                        float diff = MathHelper.WrapAngle(-MathHelper.PiOver2 - cur);
                        float turn = MathHelper.Clamp(diff, -0.16f, 0.16f);
                        float speed = Projectile.velocity.Length();
                        if (Math.Abs(diff) < 0.3f) {
                            speed = Math.Min(speed * 1.05f + 0.1f, 24f);
                        }
                        Projectile.velocity = (cur + turn).ToRotationVector2() * speed;
                    }
                    break;
                }
                case ModeDashTrail: {
                    float speed = Math.Min(Projectile.velocity.Length() * 1.045f + 0.05f, 30f);
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
                    break;
                }
                case ModeShapeBurst: {
                    //乘法加速:同一轮所有弹按相同比例外扩,形状不变
                    Projectile.velocity *= 1.012f;
                    break;
                }
                case ModeZPierce:
                case ModeZFromNear: {
                    //穿过平面后继续沿原方向飞,出视野即静默消失(不要在镜头前爆一团火花)
                    if (OutOfSight) {
                        Projectile.Kill();
                    }
                    break;
                }
                case ModeZBoomerang: {
                    Vector2 lock2 = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                    if (ZVel > 0f) {
                        //去程:沿抛出方向减速,同时慢慢朝锁定点上空拐(远处绕行的小点)
                        Vector2 toLock = lock2 - Projectile.Center;
                        float cur = Projectile.velocity.ToRotation();
                        float diff = MathHelper.WrapAngle(toLock.ToRotation() - cur);
                        float turn = MathHelper.Clamp(diff, -0.05f, 0.05f);
                        float speed = Math.Max(Projectile.velocity.Length() * 0.97f, 3f);
                        Projectile.velocity = (cur + turn).ToRotationVector2() * speed;
                    }
                    else if (Z > 0f) {
                        //回程:按剩余帧数精确收敛到锁定点,到达平面那一帧刚好压在锁点上
                        float frames = Math.Max(FramesToPlane(), 1f);
                        Projectile.velocity = (lock2 - Projectile.Center) / frames;
                    }
                    else if (OutOfSight) {
                        Projectile.Kill();
                    }
                    break;
                }
                case ModeZOrbit: {
                    //倾斜轨道螺旋:角按角速度推进、半径按帧数长;Z = 振幅 × sin 角,按差分写回 Z 速度(判定带按真实 Z 位移放宽)
                    Vector2 c = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                    float angle = Projectile.localAI[2] + VDDirector.SingOrbitAngular * age;
                    float radius = VDDirector.SingDiskRadius + VDDirector.SingOrbitRadiusGrowth * age;
                    float tiltCos = MathF.Cos(MathHelper.ToRadians(VDDirector.SingOrbitTiltDeg));
                    Vector2 next = c + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * tiltCos) * radius;
                    Projectile.velocity = next - Projectile.Center;
                    float wantZ = VDDirector.SingOrbitDepthAmp * MathF.Sin(angle);
                    ZVel = wantZ - Z;
                    ZAccel = 0f;
                    break;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (!HasDepth || Math.Abs(Z) < 0.5f) {
                Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.5f);
            }

            if (Main.dedServ || Mode == ModeShapeBurst) {
                return;
            }
            //火焰拖尾:虚空粒子走像素管线,发光火花保底;深度模式只在平面附近放粒子(粒子系统不分层,远处放了会飘在物块前)
            if (HasDepth && Math.Abs(Z) > 0.35f) {
                return;
            }
            if (age % 2 == 0) {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.Zero);
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center + back * 10f + CEUtils.randomPointInCircle(5f), back * Main.rand.NextFloat(0.5f, 2f) + Projectile.velocity * 0.2f, Color.White, Main.rand.NextFloat(0.7f, 1.1f));
                p.Opacity = 0.7f;
                p.ad = 0.04f;
            }
            if (age % 5 == 0) {
                Vector2 v = -Projectile.velocity * 0.15f + CEUtils.randomPointInCircle(1.5f);
                VDVfx.Spark(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.4f, 0.8f), 0.9f, 18);
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || (HasDepth && Math.Abs(Z) > 0.5f)) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 6f);
                VDVfx.Spark(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.5f, 0.9f), 1f, 20, gravity: true);
            }
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = ProjectedCenter - Main.screenPosition;
            float scale = DrawScale;
            float fade = Mode == ModeShapeBurst ? MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) : 1f;
            float depthAlpha = VDDepth.Alpha(Z);
            Color glow = VDDepth.Fog(GlowColor, Z);

            spriteBatch.UseAdditive();
            float speed = Projectile.velocity.Length();
            //拖尾:细长速度线,只在真的飞得快时画(旧版 26px 高的实心药丸拖在每颗弹后,叠在亮背景上就是一串粉椭圆)
            //深度模式的「速度」还包含 Z 向,按透视缩放后的表观位移估
            float apparentSpeed = HasDepth ? speed * VDDepth.Scale(Z) + Math.Abs(ZVel) * 220f * VDDepth.Scale(Z) : speed;
            if (Mode != ModeShapeBurst && apparentSpeed > 10f) {
                Texture2D streak = CEUtils.getExtraTex("StreakSolid");
                float len = MathHelper.Clamp(apparentSpeed * 3f, 24f, 60f) * Math.Max(scale, 0.35f);
                Vector2 streakDir = HasDepth ? StreakDirection() : Projectile.velocity.SafeNormalize(Vector2.Zero);
                Vector2 streakScale = new Vector2(len / streak.Width, 8f * Math.Max(scale, 0.4f) / streak.Height);
                Vector2 tailPos = drawPos - streakDir * (len * 0.5f + 6f * scale);
                spriteBatch.Draw(streak, tailPos, null, glow * (0.45f * depthAlpha), streakDir.ToRotation(), streak.Size() / 2f, streakScale, SpriteEffects.None, 0f);
            }
            if (HasDepth) {
                DrawDepthTrail(tex, origin, GlowColor, 0.35f * fade);
            }
            else {
                for (int i = 1; i < Projectile.oldPos.Length; i++) {
                    float a = (1f - i / (float)Projectile.oldPos.Length) * 0.35f * fade;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    spriteBatch.Draw(tex, pos, null, GlowColor * a, Projectile.oldRot[i], origin, Projectile.scale * (1f - i * 0.04f), SpriteEffects.None, 0f);
                }
            }
            Texture2D glowTex = CEUtils.getExtraTex("Glow");
            spriteBatch.Draw(glowTex, drawPos, null, glow * (0.6f * fade * depthAlpha), 0f, glowTex.Size() / 2f, 0.16f * scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            if (HasDepth) {
                VDDepthDraw.Draw(tex, drawPos, null, Color.White, fade, Projectile.rotation, origin, scale, SpriteEffects.None, Z);
            }
            else {
                spriteBatch.Draw(tex, drawPos, null, Color.White * fade, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>深度模式的表观运动方向:上一帧投影点到本帧投影点(Z 向运动也会在屏幕上表现为向消失点收敛/发散)</summary>
        private Vector2 StreakDirection() {
            if (trailCount < 2) {
                return Projectile.velocity.SafeNormalize(Vector2.UnitY);
            }
            Vector2 prev = VDDepth.Project(trailPos[1], trailZ[1]);
            Vector2 dir = ProjectedCenter - prev;
            return dir.LengthSquared() < 0.01f ? Projectile.velocity.SafeNormalize(Vector2.UnitY) : dir.SafeNormalize(Vector2.UnitY);
        }
    }
}
