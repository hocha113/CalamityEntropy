using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
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
    /// 3 冲刺路径遗留(沿初速方向持续加速);4 形状弹幕(直飞轻绘制、短寿命)
    /// </summary>
    public class VDVoidBolt : VDHostileProjectile
    {
        public const int ModeStraight = 0;
        public const int ModeArcWrap = 1;
        public const int ModeFanRise = 2;
        public const int ModeDashTrail = 3;
        public const int ModeShapeBurst = 4;

        public static readonly Color GlowColor = new Color(190, 60, 255);

        public int Mode => (int)Projectile.ai[0];
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

        public override void AI() {
            //首帧按模式定寿命(生成后改 timeLeft 不走同步,放在全端都会跑的首帧里)
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (Mode == ModeShapeBurst) {
                    Projectile.timeLeft = 120;
                }
                else if (Mode == ModeDashTrail) {
                    Projectile.timeLeft = 180;
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
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.5f);

            if (Main.dedServ || Mode == ModeShapeBurst) {
                return;
            }
            //火焰拖尾:虚空粒子走像素管线,发光火花保底
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
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 6f);
                VDVfx.Spark(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.5f, 0.9f), 1f, 20, gravity: true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float fade = Mode == ModeShapeBurst ? MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) : 1f;

            Main.spriteBatch.UseAdditive();
            float speed = Projectile.velocity.Length();
            //拖尾:细长速度线,只在真的飞得快时画(旧版 26px 高的实心药丸拖在每颗弹后,叠在亮背景上就是一串粉椭圆)
            if (Mode != ModeShapeBurst && speed > 10f) {
                Texture2D streak = CEUtils.getExtraTex("StreakSolid");
                float len = MathHelper.Clamp(speed * 3f, 24f, 60f);
                Vector2 streakScale = new Vector2(len / streak.Width, 8f / streak.Height);
                Vector2 tailPos = drawPos - Projectile.velocity.SafeNormalize(Vector2.Zero) * (len * 0.5f + 6f);
                Main.spriteBatch.Draw(streak, tailPos, null, GlowColor * 0.45f, Projectile.velocity.ToRotation(), streak.Size() / 2f, streakScale, SpriteEffects.None, 0f);
            }
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                float a = (1f - i / (float)Projectile.oldPos.Length) * 0.35f * fade;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, pos, null, GlowColor * a, Projectile.oldRot[i], origin, Projectile.scale * (1f - i * 0.04f), SpriteEffects.None, 0f);
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, GlowColor * (0.6f * fade), 0f, glow.Size() / 2f, 0.16f * Projectile.scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, Color.White * fade, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
