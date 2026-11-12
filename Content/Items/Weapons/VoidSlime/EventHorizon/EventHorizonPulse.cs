using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.EventHorizon
{
    //黑剑命中处的引力冲击: 黑环从 10 格外缘向中心塌缩,环扫过的敌人各吃一次伤害
    //localAI0 开场音效标记,localAI1 已过帧数
    public class EventHorizonPulse : ModProjectile
    {
        //策划: 半径 10 格,单位像素
        public const float Radius = 160f;
        //时间单位帧: 塌缩 14 帧,伤害随环走;之后余光 8 帧
        private const int CollapseFrames = 14;
        private const int FadeFrames = 8;
        //环带半宽,单位像素,判定也放宽这么多
        private const float BandHalfWidth = 14f;
        //扭曲走仓库奇点的 VDScreenFx 透镜,Boss 用 0.09;半径在环外再加 40 像素
        private const float LensStrength = 0.05f;
        private const float LensMargin = 40f;
        //塌缩时每帧从环上吸向中心的火花数与寿命帧数
        private const int InwardSparks = 2;
        private const int SparkLife = 10;
        //开场音效的同时实例上限与音量
        private const int SoundInstances = 3;
        private const float SoundVolume = 0.4f;
        //塌缩到底的一闪的直径,单位像素
        private const float FlashDiameter = 90f;

        private static readonly Color VoidDeep = new(8, 7, 12);
        private static readonly Color RingDark = new(21, 18, 32);
        private static readonly Color EdgeColor = new(223, 248, 255);

        private int Age => (int)Projectile.localAI[1];
        private float Progress => MathHelper.Clamp(Projectile.localAI[1] / CollapseFrames, 0f, 1f);
        private float RingRadius => Radius * (1f - GodDraw.EaseIn(Progress));

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = CollapseFrames + FadeFrames;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                CEUtils.PlaySound("VoidBomb", Main.rand.NextFloat(1.25f, 1.4f), Projectile.Center, SoundInstances, CEUtils.WeapSound * SoundVolume);
            }
            Projectile.localAI[1]++;
            if (Main.dedServ)
                return;
            VDScreenFx.ReportLens(Projectile.Center, LensStrength * (1f - Progress), RingRadius + LensMargin);
            if (Age > CollapseFrames)
                return;
            for (int i = 0; i < InwardSparks; i++) {
                Vector2 dir = Main.rand.NextVector2Unit();
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + dir * RingRadius, -dir * Main.rand.NextFloat(3f, 6f), EdgeColor * 0.8f, 0.035f)
                    .Configure(false, SparkLife, new Vector2(0.5f, 1.3f));
            }
        }

        public override bool? CanDamage() => Age <= CollapseFrames ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 c = Projectile.Center;
            if (targetHitbox.Distance(c) > Radius)
                return false;
            //环的内沿收到目标最远的角以内,才算被环扫到
            float farX = Math.Max(Math.Abs(targetHitbox.Left - c.X), Math.Abs(targetHitbox.Right - c.X));
            float farY = Math.Max(Math.Abs(targetHitbox.Top - c.Y), Math.Abs(targetHitbox.Bottom - c.Y));
            return RingRadius - BandHalfWidth <= MathF.Sqrt(farX * farX + farY * farY);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            //击退朝向中心,像被吸进去
            modifiers.HitDirectionOverride = target.Center.X < Projectile.Center.X ? 1 : -1;
        }

        public override bool PreDraw(ref Color lightColor) {
            float fade = Age <= CollapseFrames ? 1f : 1f - (Age - CollapseFrames) / (float)FadeFrames;
            if (fade <= 0f)
                return false;
            float r = RingRadius;
            float band = Math.Min(BandHalfWidth * (0.5f + 0.5f * (1f - Progress)), r);
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginStraightAlpha();
            //环内被吸走光的一片暗区,环本体是实体黑色
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Straight(VoidDeep, 0.35f * fade), 0f, (r + band) * 2.4f / glow.Width);
            GodDraw.RingStrip(CEUtils.getExtraTex("MegaStreakBacking2b"), Projectile.Center, r, band, GodDraw.Straight(RingDark, 0.9f * fade), 0f, MathHelper.TwoPi, 64);
            GodDraw.End();
            GodDraw.BeginAdditive();
            GodDraw.RingStrip(GodDraw.SoftLine, Projectile.Center, r + band * 0.75f, 3f, GodDraw.Add(EdgeColor, 0.9f * fade), 0f, MathHelper.TwoPi, 64);
            float flash = GodDraw.Span(Progress, 0.7f, 1f) * fade;
            if (flash > 0f)
                GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(EdgeColor, 0.8f * flash), 0f, FlashDiameter / glow.Width);
            GodDraw.End();
            return false;
        }
    }
}
