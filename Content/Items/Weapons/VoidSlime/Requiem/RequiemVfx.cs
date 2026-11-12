using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using Terraria;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    /// <summary>安息各弹幕共用的配色、火光、抽壳与拖尾,服务器上直接返回</summary>
    public static class RequiemVfx
    {
        public static readonly Color GoldDeep = new(203, 179, 73);
        public static readonly Color Gold = new(229, 214, 127);
        public static readonly Color GoldWhite = new(255, 249, 181);
        public static readonly Color Silver = new(166, 174, 187);
        public static readonly Color Ice = new(170, 204, 255);
        public static readonly Color IceWhite = new(223, 248, 255);
        public static readonly Color VoidBlue = new(85, 138, 255);
        public static readonly Color Ember = new(255, 156, 64);
        public static readonly Color Soot = new(64, 60, 76);

        public const float FlashScale = 0.12f;              //Glow2 缩放
        public const float FlashWhiten = 0.5f;              //火光向白插值比例
        public static readonly Vector2 FlashStretch = new(2.2f, 0.8f);
        public const int FlashLife = 5;                     //帧
        public const int FlashSparks = 2;
        public const float FlashSparkSpread = 0.3f;         //弧度
        public const float FlashSparkSpeedMin = 6f;         //像素/帧
        public const float FlashSparkSpeedMax = 12f;
        public const int FlashSparkLife = 8;                //帧
        public const float FlashLight = 0.7f;               //AddLight 强度
        public const float SparkScaleMin = 0.03f;           //GlowSpark 缩放
        public const float SparkScaleMax = 0.045f;
        public static readonly Vector2 SparkSquash = new(0.4f, 1.4f);

        public const float ShellBack = 30f;                 //像素,抛壳口在枪口后方,贴图 (34, 7) 对 (64, 9)
        public const float ShellUp = 2f;                    //像素,抛壳口在枪管中线上方
        public const float ShellKickMin = 3f;               //像素/帧,向上抛出
        public const float ShellKickMax = 5f;
        public const float ShellDriftMin = 1f;              //像素/帧,向后飘
        public const float ShellDriftMax = 2.5f;
        public const int ShellLife = 50;                    //帧

        public static void MuzzleFlash(Vector2 pos, Vector2 aim, Color tint, Vector2 carry) {
            if (Main.dedServ)
                return;
            PRT_ShineParticle flash = PRTLoader.NewParticle<PRT_ShineParticle>(pos, carry, Color.Lerp(tint, Color.White, FlashWhiten), FlashScale);
            flash.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, aim.ToRotation(), FlashLife);
            flash.drawScale = FlashStretch;
            Sparks(pos, aim, FlashSparks, FlashSparkSpread, FlashSparkSpeedMin, FlashSparkSpeedMax, tint, Color.White, FlashSparkLife, carry);
            Lighting.AddLight(pos, tint.ToVector3() * FlashLight);
        }

        /// <summary>沿 dir 扇形喷出的发光火花</summary>
        public static void Sparks(Vector2 pos, Vector2 dir, int count, float spread, float speedMin, float speedMax, Color a, Color b, int life, Vector2 carry = default) {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++) {
                Vector2 v = dir.RotatedByRandom(spread) * Main.rand.NextFloat(speedMin, speedMax) + carry;
                Color c = Color.Lerp(a, b, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_GlowSparkCal>(pos, v, c, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(false, life, SparkSquash, true);
            }
        }

        /// <summary>从枪口反推抛壳口,弹壳向上偏后抛出</summary>
        public static void Shell(Player owner, Vector2 muzzle, Vector2 aim, float kickScale = 1f) {
            if (Main.dedServ)
                return;
            Vector2 up = aim.RotatedBy(-MathHelper.PiOver2 * owner.direction) * owner.gravDir;
            Vector2 port = muzzle - aim * ShellBack + up * ShellUp;
            Vector2 v = (up * Main.rand.NextFloat(ShellKickMin, ShellKickMax) - aim * Main.rand.NextFloat(ShellDriftMin, ShellDriftMax)) * kickScale + owner.velocity;
            PRTLoader.NewParticle<PRT_ShellParticle>(port, v, Color.White, 1f).Configure(1f, false, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), ShellLife);
        }

        /// <summary>按 oldPos 画拖尾,A=0 在默认批次里叠加发光</summary>
        public static void DrawTrail(Projectile p, int length, Color head, Color tail, float width) {
            Vector2 half = p.Size / 2f;
            Vector2 prev = p.Center;
            int n = Math.Min(length, p.oldPos.Length);
            for (int i = 0; i < n; i++) {
                if (p.oldPos[i] == Vector2.Zero)
                    break;
                Vector2 pos = p.oldPos[i] + half;
                float k = 1f - i / (float)n;
                Color c = Color.Lerp(tail, head, k) * k;
                c.A = 0;
                CEUtils.drawLine(prev, pos, c, width * k * p.scale);
                prev = pos;
            }
        }
    }
}
